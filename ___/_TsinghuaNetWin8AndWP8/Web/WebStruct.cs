﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.Linq;

namespace Web
{
    /// <summary>
    /// 表示一定的字节数。
    /// </summary>
    public struct Size : IComparable<Size>, IEquatable<Size>
    {
        /// <summary>
        /// 表示 <see cref="Size"/> 的最小可能值。
        /// 此字段为只读。
        /// </summary>
        public static readonly Size MinValue = new Size(ulong.MinValue);

        /// <summary>
        /// 表示 <see cref="Size"/> 的最大可能值。
        /// 此字段为只读。
        /// </summary>
        public static readonly Size MaxValue = new Size(ulong.MaxValue);

        private const double kb = 1e3;
        private const double mb = 1e6;
        private const double gb = 1e9;
        private const double tb = 1e12;
        private const double pb = 1e15;

        /// <summary>
        /// 将字节数的字符串表示形式转换为它的等效 <see cref="Size"/>。
        /// </summary>
        /// <param name="value">包含要转换的数字的字符串。</param>
        /// <returns>与 <paramref name="value"/> 中指定的数值或符号等效的 <see cref="Size"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 <c>null</c>。</exception>
        /// <exception cref="FormatException"><paramref name="value"/> 不表示一个有效格式的数字。</exception>
        public static Size Parse(string value)
        {
            if(value == null)
                throw new ArgumentNullException("value");
            if(string.IsNullOrWhiteSpace(value))
                throw new FormatException("字符串格式错误。");
            switch(value[value.Length - 1])
            {
            case 'P':
                return new Size((ulong)(double.Parse(value.Substring(0, value.Length - 1), CultureInfo.InvariantCulture) * pb));
            case 'T':
                return new Size((ulong)(double.Parse(value.Substring(0, value.Length - 1), CultureInfo.InvariantCulture) * tb));
            case 'G':
                return new Size((ulong)(double.Parse(value.Substring(0, value.Length - 1), CultureInfo.InvariantCulture) * gb));
            case 'M':
                return new Size((ulong)(double.Parse(value.Substring(0, value.Length - 1), CultureInfo.InvariantCulture) * mb));
            case 'K':
                return new Size((ulong)(double.Parse(value.Substring(0, value.Length - 1), CultureInfo.InvariantCulture) * kb));
            case 'B':
                return new Size((ulong)(double.Parse(value.Substring(0, value.Length - 1), CultureInfo.InvariantCulture)));
            default:
                return new Size((ulong)(double.Parse(value, CultureInfo.InvariantCulture)));
            }
        }

        public static bool operator <=(Size size1, Size size2)
        {
            return size1.Value <= size2.Value;
        }

        public static bool operator >=(Size size1, Size size2)
        {
            return size1.Value >= size2.Value;
        }

        public static bool operator <(Size size1, Size size2)
        {
            return size1.Value < size2.Value;
        }

        public static bool operator >(Size size1, Size size2)
        {
            return size1.Value > size2.Value;
        }

        public static Size operator +(Size size1, Size size2)
        {
            return new Size(size1.Value + size2.Value);
        }

        public static Size operator *(Size size1, double d2)
        {
            return new Size((ulong)(size1.Value * d2));
        }

        public static Size operator /(Size size1, double d2)
        {
            return new Size((ulong)(size1.Value / d2));
        }

        public static Size operator -(Size size1, Size size2)
        {
            if(size1 < size2)
                throw new OverflowException("size1 < size2");
            return new Size(size1.Value - size2.Value);
        }

        public static bool operator ==(Size size1, Size size2)
        {
            return size1.Value == size2.Value;
        }

        public static bool operator !=(Size size1, Size size2)
        {
            return size1.Value != size2.Value;
        }

        /// <summary>
        /// 指示此实例与指定对象是否相等。
        /// </summary>
        /// <param name="obj">要与当前实例进行比较的对象。</param>
        /// <returns>
        /// 如果 <paramref name="obj"/> 和该实例具有相同的类型并表示相同的值，则为 <c>true</c>；否则为 <c>false</c>。
        /// </returns>
        public override bool Equals(object obj)
        {
            if(obj is Size)
                return this.Equals((Size)obj);
            else
                return false;
        }

        /// <summary>
        /// 返回此实例的哈希代码。
        /// </summary>
        /// <returns>一个 32 位有符号整数，它是该实例的哈希代码。</returns>
        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        /// <summary>
        /// 两个指定的 <see cref="Size"/> 值相加。
        /// </summary>
        /// <param name="size1">要相加的第一个值。</param>
        /// <param name="size2">要相加的第二个值。</param>
        /// <returns><paramref name="size1"/> 与 <paramref name="size2"/> 之和。</returns>
        public static Size Add(Size size1, Size size2)
        {
            return size1 + size2;
        }

        /// <summary>
        /// 从一个 <see cref="Size"/> 值中减去指定的另一个这种类型的值。
        /// </summary>
        /// <param name="size1">被减数。</param>
        /// <param name="size2">减数。</param>
        /// <returns><paramref name="size1"/> 减去 <paramref name="size2"/> 所得的结果。</returns>
        /// <exception cref="Systm.OverFlowException"><paramref name="size1"/> 小于 <paramref name="size2"/>。</exception>
        public static Size Subtract(Size size1, Size size2)
        {
            return size1 - size2;
        }

        /// <summary>
        /// 创建 <see cref="Size"/> 的新实例。
        /// </summary>
        /// <param name="sizeString">要存储的字节数。</param>
        public Size(ulong value)
            : this()
        {
            this.Value = value;
        }

        /// <summary>
        /// 存储的值。
        /// </summary>
        public ulong Value
        {
            get;
            set;
        }

        public double TotalGB
        {
            get
            {
                return Value / gb;
            }
        }

        /// <summary>
        /// 返回当前对象的字符串形式。
        /// </summary>
        /// <returns>当前对象的字符串形式。</returns>
        public override string ToString()
        {
            var culture = CultureInfo.CurrentCulture;
            var ds = culture.NumberFormat.NumberDecimalSeparator;
            var va = Value;
            Func<double, string> format = value =>
            {
                return value.ToString("##0" + ds + "00", culture);
            };
            if(va < kb)
                return va.ToString(culture) + " B";
            if(va < mb)
                return format(va / kb) + " KB";
            if(va < gb)
                return format(va / mb) + " MB";
            if(va < tb)
                return format(va / gb) + " GB";
            if(va < pb)
                return format(va / tb) + " TB";
            return format(va / pb) + " PB";
        }

        public int CompareTo(Size other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(Size other)
        {
            return this == other;
        }
    }

    /// <summary>
    /// 表示特定的 Mac 地址。
    /// </summary>
    public struct MacAddress : IEquatable<MacAddress>
    {
        private static readonly char[] splitChars = ":. ".ToCharArray();

        /// <summary>
        /// 将 Mac 地址的字符串表示形式转换为它的等效 <see cref="MacAddress"/>。
        /// </summary>
        /// <param name="sizeString">包含要转换的 Mac 地址的字符串。</param>
        /// <returns>与 <paramref name="sizeString"/> 中指定的 <see cref="MacAddress"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="sizeString"/> 为 <c>null</c>。</exception>
        /// <exception cref="System.FormatException"><paramref name="sizeString"/> 不表示一个有效格式的 Mac 地址。</exception>
        public static MacAddress Parse(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
                return Unknown;
            var mac = value.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
            if(mac.Length != 6)
                throw new FormatException("字符串格式有误。");
            var result = new MacAddress();
            try
            {
                for(int i = 0; i < 6; i++)
                    result[i] = byte.Parse(mac[i], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
            }
            catch(Exception ex)
            {
                throw new FormatException("字符串格式有误。", ex);
            }
            return result;
        }

        /// <summary>
        /// 表示本机的 <see cref="MacAddress"/> 对象。
        /// 此字段为只读。
        /// </summary>
        public static readonly MacAddress Current = initCurrentMac();

        private static MacAddress initCurrentMac()
        {
            object mac;
            if(Windows.Storage.ApplicationData.Current.LocalSettings.Values.TryGetValue("Mac", out mac))
            {
                return Parse(mac.ToString());
            }
            else
            {
                var bytes = new byte[6];
                new Random().NextBytes(bytes);
                var re = new MacAddress(bytes);
                Windows.Storage.ApplicationData.Current.LocalSettings.Values.Add("Mac", re.ToString());
                return re;
            }
        }

        /// <summary>
        /// 表示未知的 <see cref="MacAddress"/>。
        /// 此字段为只读。
        /// </summary>
        public static readonly MacAddress Unknown = new MacAddress();

        /// <summary>
        /// 通过字节数组创建 <see cref="MacAddress"/> 的新实例。
        /// </summary>
        /// <param name="sizeString">长度为 6 的 <see cref="System.Byte[]"/>，表示一个 Mac 地址。</param>
        /// <exception cref="ArgumentNullException"><paramref name="sizeString"/> 为 <c>null</c>。</exception>
        /// <exception cref="System.ArgumentException"><paramref name="sizeString"/> 长度不为 6。</exception>
        public MacAddress(params byte[] value)
        {
            if(value == null)
                throw new ArgumentNullException("value");
            if(value.Length != 6)
                throw new ArgumentException("数组长度应为 6。", "value");
            this.value0 = value[0];
            this.value1 = value[1];
            this.value2 = value[2];
            this.value3 = value[3];
            this.value4 = value[4];
            this.value5 = value[5];
        }

        /// <summary>
        /// 获取或设置 <see cref="MacAddress"/> 的值。
        /// </summary>
        /// <param name="index">索引，0~5。</param>
        /// <returns><see cref="MacAddress"/> 相应位的值。</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index"/> 超出索引范围。</exception>
        public byte this[int index]
        {
            get
            {
                switch(index)
                {
                case 0:
                    return value0;
                case 1:
                    return value1;
                case 2:
                    return value2;
                case 3:
                    return value3;
                case 4:
                    return value4;
                case 5:
                    return value5;
                default:
                    throw new ArgumentOutOfRangeException("index", "index 应为0~5。");
                }
            }
            private set
            {
                switch(index)
                {
                case 0:
                    value0 = value;
                    break;
                case 1:
                    value1 = value;
                    break;
                case 2:
                    value2 = value;
                    break;
                case 3:
                    value3 = value;
                    break;
                case 4:
                    value4 = value;
                    break;
                case 5:
                    value5 = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("index", "index 应为0~5。");
                }
            }
        }

        private byte value0, value1, value2, value3, value4, value5;

        /// <summary>
        /// 获取一个值，指示当前 <see cref="MacAddress"/> 是否与 <see cref="MacAddress.Current"/> 相等。
        /// </summary>
        public bool IsCurrent
        {
            get
            {
                return this == Current;
            }
        }

        /// <summary>
        /// 返回当前 <see cref="MacAddress"/> 的字符串形式。
        /// </summary>
        /// <returns>当前 <see cref="MacAddress"/> 的字符串形式，以 ":" 分隔。</returns>
        public override string ToString()
        {
            return $"{value0:x2}:{value1:x2}:{value2:x2}:{value3:x2}:{value4:x2}:{value5:x2}";
        }

        /// <summary>
        /// 指示此实例与指定对象是否相等。
        /// </summary>
        /// <param name="obj">要与当前实例进行比较的对象。</param>
        /// <returns>
        /// 如果 <paramref name="obj"/> 和该实例具有相同的类型并表示相同的值，则为 <c>true</c>；否则为 <c>false</c>。
        /// </returns>
        public override bool Equals(object obj)
        {
            if(obj is MacAddress)
                return this.Equals((MacAddress)obj);
            else
                return false;
        }

        /// <summary>
        /// 返回此实例的哈希代码。
        /// </summary>
        /// <returns>一个 32 位有符号整数，它是该实例的哈希代码。</returns>
        public override int GetHashCode()
        {
            int re = (((value2 << 8) + value3 << 8) + value4 << 8) + value5;
            int re2 = value0 + (value1 << 8);
            return re ^ re2;
        }

        public bool Equals(MacAddress other)
        {
            return value0 == other.value0 && value1 == other.value1 && value2 == other.value2 && value3 == other.value3 && value4 == other.value4 && value5 == other.value5;
        }

        public static bool operator ==(MacAddress mac1, MacAddress mac2)
        {
            return mac1.Equals(mac2);
        }

        public static bool operator !=(MacAddress mac1, MacAddress mac2)
        {
            return !mac1.Equals(mac2);
        }
    }

    /// <summary>
    /// 表示特定的 Ipv4 地址。
    /// </summary>
    public struct Ipv4Address : IEquatable<Ipv4Address>
    {
        private static readonly char[] splitChars = ".: ".ToCharArray();

        /// <summary>
        /// 将 IP 地址的字符串表示形式转换为它的等效 <see cref="Ipv4Address"/>。
        /// </summary>
        /// <param name="sizeString">包含要转换的 IP 地址的字符串。</param>
        /// <returns>与 <paramref name="sizeString"/> 中指定的 <see cref="Ipv4Address"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="sizeString"/> 为 <c>null</c>。</exception>
        /// <exception cref="System.FormatException"><paramref name="sizeString"/> 不表示一个有效格式的 IP 地址。</exception>
        public static Ipv4Address Parse(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException("value");
            var ip = value.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
            if(ip.Length != 4)
                throw new FormatException("字符串格式有误。");
            var result = new Ipv4Address();
            try
            {
                for(int i = 0; i < 4; i++)
                    result[i] = byte.Parse(ip[i], CultureInfo.InvariantCulture);
            }
            catch(Exception ex)
            {
                throw new FormatException("字符串格式有误。", ex);
            }
            return result;
        }

        /// <summary>
        /// 通过字节数组创建 <see cref="Ipv4Address"/> 的新实例。
        /// </summary>
        /// <param name="sizeString">长度为 4 的 <see cref="byte[]"/>，表示一个 IP 地址。</param>
        /// <exception cref="ArgumentNullException"><paramref name="sizeString"/> 为 <c>null</c>。</exception>
        /// <exception cref="System.ArgumentException"><paramref name="sizeString"/> 长度不为 4。</exception>
        public Ipv4Address(params byte[] value)
        {
            if(value == null)
                throw new ArgumentNullException("value");
            if(value.Length != 4)
                throw new ArgumentException("数组长度应为 4。", "value");
            this.value0 = value[0];
            this.value1 = value[1];
            this.value2 = value[2];
            this.value3 = value[3];
        }

        /// <summary>
        /// 获取或设置 <see cref="Ipv4Address"/> 的值。
        /// </summary>
        /// <param name="index">索引，0~3。</param>
        /// <returns><see cref="Ipv4Address"/> 相应位的值。</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index"/> 超出索引范围。</exception>
        public byte this[int index]
        {
            get
            {
                switch(index)
                {
                case 0:
                    return value0;
                case 1:
                    return value1;
                case 2:
                    return value2;
                case 3:
                    return value3;
                default:
                    throw new ArgumentOutOfRangeException("index", "index 应为0~3。");
                }
            }
            private set
            {
                switch(index)
                {
                case 0:
                    value0 = value;
                    break;
                case 1:
                    value1 = value;
                    break;
                case 2:
                    value2 = value;
                    break;
                case 3:
                    value3 = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("index", "index 应为0~3。");
                }
            }
        }

        private byte value0, value1, value2, value3;

        /// <summary>
        /// 返回当前 <see cref="Ipv4Address"/> 的字符串形式。
        /// </summary>
        /// <returns>当前 <see cref="Ipv4Address"/> 的字符串形式，以 "." 分隔。</returns>
        public override string ToString()
        {
            return string.Join(".", value0.ToString(CultureInfo.InvariantCulture), value1.ToString(CultureInfo.InvariantCulture), value2.ToString(CultureInfo.InvariantCulture), value3.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 指示此实例与指定对象是否相等。
        /// </summary>
        /// <param name="obj">要与当前实例进行比较的对象。</param>
        /// <returns>
        /// 如果 <paramref name="obj"/> 和该实例具有相同的类型并表示相同的值，则为 <c>true</c>；否则为 <c>false</c>。
        /// </returns>
        public override bool Equals(object obj)
        {
            if(obj is Ipv4Address)
                return this.Equals((Ipv4Address)obj);
            else
                return false;
        }

        public bool Equals(Ipv4Address other)
        {
            return value0 == other.value0 && value1 == other.value1 && value2 == other.value2 && value3 == other.value3;
        }

        /// <summary>
        /// 返回此实例的哈希代码。
        /// </summary>
        /// <returns>一个 32 位有符号整数，它是该实例的哈希代码。</returns>
        public override int GetHashCode()
        {
            int re = value0;
            re = re << 8 + value1;
            re = re << 8 + value2;
            re = re << 8 + value3;
            return re;
        }

        public static bool operator ==(Ipv4Address ip1, Ipv4Address ip2)
        {
            return ip1.Equals(ip2);
        }

        public static bool operator !=(Ipv4Address ip1, Ipv4Address ip2)
        {
            return !ip1.Equals(ip2);
        }
    }
}