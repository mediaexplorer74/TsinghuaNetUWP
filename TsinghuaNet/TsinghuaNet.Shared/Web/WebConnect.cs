﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Html;

namespace TsinghuaNet.Web
{
    /// <summary>
    /// 表示当前认证状态，并提供相关方法的类。
    /// </summary>
    public sealed class WebConnect : INotifyPropertyChanged
    {
        /// <summary>
        /// 使用用户名和加密后的密码创建新实例。
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="passwordMD5">MD5 加密后的密码，请使用 <see cref="TsinghuaNet.MD5.MDString(string)"/> 方法进行加密。</param>
        /// <exception cref="System.ArgumentNullException">参数为 <c>null</c> 或 <see cref="string.Empty"/>。</exception>
        public WebConnect(string userName, string passwordMD5)
        {
            if(string.IsNullOrEmpty(userName))
                throw new ArgumentNullException("userName");
            if(string.IsNullOrEmpty(passwordMD5))
                throw new ArgumentNullException("passwordMD5");
            this.userName = userName;
            this.passwordMd5 = passwordMD5;
            this.deviceList = new ObservableCollection<WebDevice>();
            this.DeviceList = new ReadOnlyObservableCollection<WebDevice>(this.deviceList);
        }

        private static WebConnect current;

        public static WebConnect Current
        {
            set
            {
                if(value == null)
                    throw new ArgumentNullException("value");
                WebConnect.current = value;
            }
            get
            {
                return WebConnect.current;
            }
        }

        private string userName, passwordMd5;

        /// <summary>
        /// 异步登陆网络。
        /// </summary>
        /// <exception cref="TsinghuaNet.WebConnect.LogOnException">在登陆过程中发生错误。</exception>
        public async Task LogOnAsync()
        {
            string res = null;
            using(var http = new HttpClient())
            {
                Func<string, Task<bool>> check = async toPost =>
                    {
                        try
                        {
                            res = await http.PostStrAsync("http://net.tsinghua.edu.cn/cgi-bin/do_login", toPost);
                        }
                        catch(Exception ex)
                        {
                            throw new LogOnException(LogOnExceptionType.ConnectError, ex);
                        }
                        if(Regex.IsMatch(res, @"^\d+,"))
                        {
                            var a = res.Split(',');
                            this.WebTraffic = new Size(ulong.Parse(a[2], System.Globalization.CultureInfo.InvariantCulture));
                            this.IsOnline = true;
                            return true;
                        }
                        return false;
                    };
                if(await check("action=check_online"))
                    return;
                if(await check("username=" + userName + "&password=" + passwordMd5 + "&mac=" + MacAddress.Current + "&drop=0&type=1&n=100"))
                    return;
                this.IsOnline = false;
                if((Regex.IsMatch(res, @"^password_error@\d+")))
                    throw new LogOnException(LogOnExceptionType.PasswordError);
                else
                    throw LogOnException.GetByErrorString(res);
            }
        }

        private async Task signInUsereg(HttpClient http)
        {
            bool needRetry = false;
            Func<Task> signIn = async () =>
                {
                    var logOnRes = await http.PostStrAsync("https://usereg.tsinghua.edu.cn/do.php", "action=login&user_login_name=" + userName + "&user_password=" + passwordMd5);
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
            catch(Exception ex)
            {
                throw new LogOnException(LogOnExceptionType.ConnectError, ex);
            }
            if(!needRetry)
                return;
            await Task.Delay(500);
            await signIn();//重试}
        }

        /// <summary>
        /// 异步请求更新状态。
        /// </summary>
        public async Task RefreshAsync()
        {
            var http = new HttpClient(new HttpClientHandler(), true);
            try
            {
                await signInUsereg(http);
                //获取用户信息
                var res1 = HtmlUtilities.ConvertToText(await http.GetStrAsync("https://usereg.tsinghua.edu.cn/user_info.php"));
                var info1 = Regex.Match(res1, @"使用流量\(IPV4\)(\d+)\(byte\).+帐户余额(.+)\(元\)", RegexOptions.Singleline).Groups;
                if(info1.Count != 3)
                {
                    var ex = new InvalidOperationException("获取到的数据格式错误。");
                    ex.Data.Add("HtmlResponse", res1);
                    throw ex;
                }
                WebTraffic = new Size(ulong.Parse(info1[1].Value, System.Globalization.CultureInfo.InvariantCulture));
                Balance = decimal.Parse(info1[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                //获取登录信息
                var res2 = await http.GetStrAsync("https://usereg.tsinghua.edu.cn/online_user_ipv4.php");
                var info2 = Regex.Matches(res2, "<tr align=\"center\">.+?</tr>", RegexOptions.Singleline);
                var devices = from Match r in info2
                              let details = Regex.Matches(r.Value, "(?<=\\<td class=\"maintd\"\\>)(.+?)(?=\\</td\\>)")
                              select new WebDevice(Ipv4Address.Parse(details[3].Value),
                                                  Size.Parse(details[4].Value),
                                                  MacAddress.Parse(details[17].Value),
                                                  DateTime.ParseExact(details[14].Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                                                  Regex.Match(r.Value, "(?<=drop\\('" + details[3].Value + "',')(.+?)(?='\\))").Value,
                                                  http);
                deviceList.Clear();
                foreach(var item in devices)
                {
                    deviceList.Add(item);
                }
                //全部成功
                UpdateTime = DateTime.Now;
            }
            catch(LogOnException)
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
                var sum = webTraffic;
                foreach(var item in deviceList)
                    sum += item.WebTraffic;
                return sum;
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
        private void propertyChanging([CallerMemberName]string propertyName = "")
        {
            if(PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}