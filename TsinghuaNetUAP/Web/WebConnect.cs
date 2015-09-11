﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Windows.Web.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.WindowsRuntime.AsyncInfo;
using Windows.Foundation;
using Windows.Web.Http.Headers;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.Data.Json;
using System.IO;

namespace Web
{
    /// <summary>
    /// 表示当前认证状态，并提供相关方法的类。
    /// </summary>
    public sealed class WebConnect : INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// 使用用户名和加密后的密码创建新实例。
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="password">密码</param>
        /// <exception cref="ArgumentNullException">参数为 <c>null</c> 或 <see cref="string.Empty"/>。</exception>
        public WebConnect(string userName, string password)
            : this(new PasswordCredential("TsinghuaAllInOne", userName, password))
        {
        }

        /// <summary>
        /// 使用凭据创建实例。
        /// </summary>
        /// <param name="account">帐户凭据</param>
        /// <exception cref="ArgumentNullException">参数为 <c>null</c>。</exception>
        /// <exception cref="ArgumentException">参数错误。</exception>
        public WebConnect(PasswordCredential account)
        {
            if(account == null)
                throw new ArgumentNullException(nameof(account));
            if(string.IsNullOrEmpty(account.UserName))
                throw new ArgumentException(nameof(account));
            this.userName = account.UserName;
            account.RetrievePassword();
            this.password = account.Password;
            this.passwordMd5 = MD5Helper.GetMd5Hash(password);
            this.deviceList = new ObservableCollection<WebDevice>();
            this.DeviceList = new ReadOnlyObservableCollection<WebDevice>(this.deviceList);
        }

        public PasswordCredential Account => new PasswordCredential("TsinghuaAllInOne", userName, password);

        private static WebConnect current;

        public static WebConnect Current
        {
            set
            {
                if(value == null)
                    throw new ArgumentNullException("value");
                current = value;
            }
            get
            {
                return current;
            }
        }

        public IAsyncAction LoadCache()
        {
            return Run(async token =>
            {
                var cacheFolder = ApplicationData.Current.LocalCacheFolder;
                try
                {
                    using(var stream = await cacheFolder.OpenStreamForReadAsync("WebConnectCache.json"))
                    using(var reader = new StreamReader(stream))
                    {
                        var dataStr = reader.ReadToEnd();
                        if(string.IsNullOrWhiteSpace(dataStr))
                            return;
                        JsonObject data;
                        if(!JsonObject.TryParse(dataStr, out data))
                            return;
                        Balance = (decimal)data[nameof(Balance)].GetNumber();
                        UpdateTime = DateTime.FromBinary((long)data[nameof(UpdateTime)].GetNumber());
                        WebTraffic = new Size((ulong)data[nameof(WebTraffic)].GetNumber());
                        var devices = from item in data[nameof(DeviceList)].GetArray()
                                      let device = item.GetObject()
                                      let ip = Ipv4Address.Parse(device[nameof(WebDevice.IPAddress)].GetString())
                                      let mac = MacAddress.Parse(device[nameof(WebDevice.Mac)].GetString())
                                      let deTraffic = new Size((ulong)device[nameof(WebDevice.WebTraffic)].GetNumber())
                                      let deTime = DateTime.FromBinary((long)device[nameof(WebDevice.LogOnDateTime)].GetNumber())
                                      select new WebDevice(ip, mac)
                                      {
                                          WebTraffic = deTraffic,
                                          LogOnDateTime = deTime
                                      };
                        deviceList.FirstOrDefault()?.HttpClient?.Dispose();
                        foreach(var item in deviceList)
                        {
                            item.Dispose();
                        }
                        deviceList.Clear();
                        foreach(var item in devices)
                        {
                            deviceList.Add(item);
                        }
                    }
                }
                catch(IOException) { return; }
            });
        }

        public IAsyncAction SaveCache()
        {
            return Run(async token =>
            {
                var data = new JsonObject();
                data[nameof(Balance)] = JsonValue.CreateNumberValue((double)this.balance);
                data[nameof(UpdateTime)] = JsonValue.CreateNumberValue(this.updateTime.ToBinary());
                data[nameof(WebTraffic)] = JsonValue.CreateNumberValue(this.webTraffic.Value);
                var devices = new JsonArray();
                foreach(var item in this.deviceList)
                {
                    var device = new JsonObject();
                    device[nameof(WebDevice.IPAddress)] = JsonValue.CreateStringValue(item.IPAddress.ToString());
                    device[nameof(WebDevice.Mac)] = JsonValue.CreateStringValue(item.Mac.ToString());
                    device[nameof(WebDevice.WebTraffic)] = JsonValue.CreateNumberValue(item.WebTraffic.Value);
                    device[nameof(WebDevice.LogOnDateTime)] = JsonValue.CreateNumberValue(item.LogOnDateTime.ToBinary());
                    devices.Add(device);
                }
                data[nameof(DeviceList)] = devices;
                var cacheFolder = ApplicationData.Current.LocalCacheFolder;
                var cacheFile = await cacheFolder.CreateFileAsync("WebConnectCache.json", CreationCollisionOption.ReplaceExisting);
                using(var stream = await cacheFile.OpenStreamForWriteAsync())
                using(var writer = new StreamWriter(stream))
                {
                    writer.Write(data.Stringify());
                }
            });
        }

        private readonly string userName, password, passwordMd5;

        private static readonly Uri logOnUri = new Uri("http://net.tsinghua.edu.cn/do_login.php");

        /// <summary>
        /// 异步登陆网络。
        /// </summary>
        /// <exception cref="WebConnect.LogOnException">在登陆过程中发生错误。</exception>
        public IAsyncAction LogOnAsync()
        {
            return LogOnAsync(true);
        }

        /// <summary>
        /// 异步登陆网络。
        /// </summary>
        /// <param name="checkIfOnline">是否检查链接可用性。</param>
        /// <exception cref="WebConnect.LogOnException">在登陆过程中发生错误。</exception>
        public IAsyncAction LogOnAsync(bool checkIfOnline)
        {
            return Run(async token =>
            {
                string res = null;
                IAsyncOperation<string> post = null;
                token.Register(() => post?.Cancel());
                using(var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.UserAgent.Add(new HttpProductInfoHeaderValue("Mozilla", "5.0"));
                    http.DefaultRequestHeaders.UserAgent.Add(new HttpProductInfoHeaderValue("Windows NT 10.0"));
                    if(checkIfOnline && await http.CheckLinkAvailable())
                        return;
                    Func<Task<bool>> check = async () =>
                            {
                                try
                                {
                                    post = http.PostStrAsync(logOnUri, "action=check_online");
                                    return "online" == await post;
                                }
                                catch(OperationCanceledException)
                                {
                                    throw;
                                }
                                catch(Exception ex)
                                {
                                    throw new LogOnException(LogOnExceptionType.ConnectError, ex);
                                }
                            };
                    Func<Task<bool>> logOn = async () =>
                    {
                        try
                        {
                            post = http.PostStrAsync(logOnUri, $"action=login&username={userName}&password={{MD5_HEX}}{passwordMd5}&type=1&ac_id=1&mac={MacAddress.Current}");
                            //post = http.PostStrAsync(new Uri("http://166.111.204.120:69/cgi-bin/srun_portal"), $"action=login&username={userName}&password={passwordMd5}&drop=0&pop=0&type=2&n=117&mbytes=0&minutes=0&ac_id=1&mac={MacAddress.Current}&chap=1");
                            res = await post;
                            if(!res.StartsWith("E"))
                                return true;
                            else
                                throw LogOnException.GetByErrorString(res);
                        }
                        catch(OperationCanceledException)
                        {
                            throw;
                        }
                        catch(LogOnException)
                        {
                            throw;
                        }
                        catch(Exception ex)
                        {
                            throw new LogOnException(LogOnExceptionType.ConnectError, ex);
                        }
                    };
                    try
                    {
                        if(this.IsOnline = await check())
                            return;
                        if(this.IsOnline = await logOn())
                            return;
                        this.IsOnline = false;
                    }
                    catch(OperationCanceledException)
                    {
                        return;
                    }
                }
            });
        }

        private IAsyncAction signInUsereg(HttpClient http)
        {
            return Run(async token =>
            {
                bool needRetry = false;
                IAsyncOperation<string> postAction = null;
                token.Register(() => postAction?.Cancel());
                Func<Task> signIn = async () =>
                    {
                        postAction = http.PostStrAsync(new Uri("http://usereg.tsinghua.edu.cn/do.php"), "action=login&user_login_name=" + userName + "&user_password=" + passwordMd5);
                        var logOnRes = await postAction;
                        switch(logOnRes)
                        {
                        case "ok":
                            break;
                        case "用户不存在":
                            throw new LogOnException(LogOnExceptionType.UserNameError);
                        case "密码错误":
                            throw new LogOnException(LogOnExceptionType.PasswordError);
                        default:
                            throw new LogOnException(logOnRes);
                        }
                    };
                try
                {
                    await signIn();
                }
                catch(LogOnException ex)
                {
                    if(ex.ExceptionType == LogOnExceptionType.UnknownError)
                    {
                        needRetry = true;
                    }
                    else
                    {
                        throw;
                    }
                }
                catch(OperationCanceledException)
                {
                    throw;
                }
                catch(Exception ex)
                {
                    throw new LogOnException(LogOnExceptionType.ConnectError, ex);
                }
                if(!needRetry)
                    return;
                await Task.Delay(500);
                await signIn();//重试
            });
        }

        private struct deviceComparer : IEqualityComparer<WebDevice>
        {
            #region IEqualityComparer<WebDevice> 成员

            public bool Equals(WebDevice x, WebDevice y)
            {
                return x.IPAddress == y.IPAddress && x.Mac == y.Mac;
            }

            public int GetHashCode(WebDevice obj)
            {
                return obj.IPAddress.GetHashCode();
            }

            #endregion
        }

        /// <summary>
        /// 异步请求更新状态。
        /// </summary>
        public IAsyncAction RefreshAsync()
        {
            return Run(async token =>
            {
                var http = new HttpClient();
                IAsyncAction act = null;
                IAsyncOperation<string> ope = null;
                token.Register(() =>
                {
                    act?.Cancel();
                    ope?.Cancel();
                });
                try
                {
                    act = signInUsereg(http);
                    await act;
                    //获取用户信息
                    ope = http.GetStrAsync(new Uri("http://usereg.tsinghua.edu.cn/user_info.php"));
                    var res1 = await ope;
                    var info1 = Regex.Match(res1, "使用流量\\(IPV4\\).+?(\\d+?)\\(byte\\).+?帐户余额.+?([0-9.]+)\\(元\\)", RegexOptions.Singleline).Groups;
                    if(info1.Count != 3)
                    {
                        var ex = new InvalidOperationException("获取到的数据格式错误。");
                        ex.Data.Add("HtmlResponse", res1);
                        throw ex;
                    }
                    WebTraffic = new Size(ulong.Parse(info1[1].Value, CultureInfo.InvariantCulture));
                    Balance = decimal.Parse(info1[2].Value, CultureInfo.InvariantCulture);
                    //获取登录信息
                    var res2 = await http.GetStrAsync(new Uri("http://usereg.tsinghua.edu.cn/online_user_ipv4.php"));
                    var info2 = Regex.Matches(res2, "<tr align=\"center\">.+?</tr>", RegexOptions.Singleline);
                    var devices = (from Match r in info2
                                   let details = Regex.Matches(r.Value, "(?<=\\<td class=\"maintd\"\\>)(.*?)(?=\\</td\\>)")
                                   let ip = Ipv4Address.Parse(details[0].Value)
                                   let mac = MacAddress.Parse(details[6].Value)
                                   let tra = Size.Parse(details[2].Value)
                                   let logOnTime = DateTime.ParseExact(details[1].Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                                   let devToken = Regex.Match(r.Value, @"(?<=value="")(\d+)(?="")").Value
                                   select new WebDevice(ip, mac)
                                   {
                                       WebTraffic = tra,
                                       LogOnDateTime = logOnTime,
                                       DropToken = devToken,
                                       HttpClient = http
                                   }).ToArray();
                    deviceList.FirstOrDefault()?.HttpClient?.Dispose();
                    var common = devices.Join(deviceList, n => n, o => o, (n, o) =>
                    {
                        o.DropToken = n.DropToken;
                        o.HttpClient = n.HttpClient;
                        o.LogOnDateTime = n.LogOnDateTime;
                        o.WebTraffic = n.WebTraffic;
                        return o;
                    }, new deviceComparer());
                    for(int i = 0; i < deviceList.Count;)
                    {
                        if(!common.Contains(deviceList[i]))
                        {
                            deviceList[i].Dispose();
                            deviceList.RemoveAt(i);
                        }
                        else
                            i++;
                    }
                    foreach(var item in devices)
                    {
                        if(!common.Contains(item, new deviceComparer()))
                            deviceList.Add(item);
                        else
                            item.Dispose();
                    }
                    //全部成功
                    UpdateTime = DateTime.Now;
                }
                catch(LogOnException)
                {
                    throw;
                }
                catch(OperationCanceledException)
                {
                    throw;
                }
                catch(Exception ex)
                {
                    throw new LogOnException(LogOnExceptionType.ConnectError, ex);
                }
                finally
                {
                    if(deviceList.Count == 0)
                        http.Dispose();
                }
            });
        }

        public string UserName
        {
            get
            {
                return this.userName;
            }
        }

        private ObservableCollection<WebDevice> deviceList;

        /// <summary>
        /// 使用该账户的设备列表。
        /// </summary>
        public ReadOnlyObservableCollection<WebDevice> DeviceList
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前连接的状态。
        /// </summary>
        public bool IsOnline
        {
            get
            {
                return isOnline;
            }
            private set
            {
                isOnline = value;
                propertyChanging();
            }
        }

        private bool isOnline = false;

        /// <summary>
        /// 当前账户余额。
        /// </summary>
        public decimal Balance
        {
            get
            {
                return balance;
            }
            private set
            {
                balance = value;
                propertyChanging();
            }
        }

        private decimal balance;

        /// <summary>
        /// 之前累积的的网络流量（不包括当前在线设备产生的流量）。
        /// </summary>
        public Size WebTraffic
        {
            get
            {
                return webTraffic;
            }
            private set
            {
                webTraffic = value;
                propertyChanging();
            }
        }

        private Size webTraffic;

        /// <summary>
        /// 精确的网络流量（包括当前在线设备产生的流量）。
        /// </summary>
        public Size WebTrafficExact
        {
            get
            {
                if(deviceList == null || deviceList.Count == 0)
                    return webTraffic;
                else
                    return deviceList.Aggregate(webTraffic, (sum, item) => sum + item.WebTraffic);
            }
        }

        private DateTime updateTime;

        /// <summary>
        /// 信息更新的时间。
        /// </summary>
        public DateTime UpdateTime
        {
            get
            {
                return updateTime;
            }
            private set
            {
                updateTime = value;
                propertyChanging();
                propertyChanging("WebTrafficExact");
            }
        }

        #region INotifyPropertyChanged 成员

        /// <summary>
        /// 属性更改时引发。
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 引发 <see cref="PropertyChanged"/> 事件。
        /// </summary>
        /// <param name="propertyName">更改的属性名，默认值表示调用方名称。</param>
        private async void propertyChanging([CallerMemberName] string propertyName = "")
        {
            await DispatcherHelper.Run(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }

        #endregion

        #region IDisposable Support

        void IDisposable.Dispose()
        {
            foreach(var item in deviceList)
            {
                item.Dispose();
            }
        }

        #endregion
    }
}