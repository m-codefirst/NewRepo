using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TRM.Web.Services.Coupons
{
    // Source:
    // https://github.com/rebeccapowell/csharp-algorithm-coupon-code/tree/master/src/Powells.CouponCode

    public class Options
    {
        public int Parts { get; set; }
        public int PartLength { get; set; }
        public string Plaintext { get; set; }

        public Options()
        {
            this.Parts = 3;
            this.PartLength = 4;
            this.Plaintext = "default-text";
        }
    }

    /// <summary>
    /// The secure random.
    /// </summary>
    public class SecureRandom : RandomNumberGenerator
    {
        /// <summary>
        /// The random number generator.
        /// </summary>
        private readonly RandomNumberGenerator rng = new RNGCryptoServiceProvider();

        /// <summary>
        /// The next.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public int Next()
        {
            var data = new byte[sizeof(int)];
            this.rng.GetBytes(data);
            return BitConverter.ToInt32(data, 0) & (int.MaxValue - 1);
        }

        /// <summary>
        /// The next.
        /// </summary>
        /// <param name="maxValue">
        /// The max value.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public int Next(int maxValue)
        {
            return this.Next(0, maxValue);
        }

        /// <summary>
        /// The next.
        /// </summary>
        /// <param name="minValue">The min value.</param>
        /// <param name="maxValue">The max value.</param>
        /// <returns>
        /// The <see cref="int" />.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">minValue cannot be greater than maxValue</exception>
        public int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException("minValue", minValue, "minValue cannot be greater than maxValue");
            }

            return (int)Math.Floor((minValue + ((double)maxValue) - minValue) * this.NextDouble());
        }

        /// <summary>
        /// The next double.
        /// </summary>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public double NextDouble()
        {
            var data = new byte[sizeof(uint)];
            this.rng.GetBytes(data);
            var randUint = BitConverter.ToUInt32(data, 0);
            return randUint / (uint.MaxValue + 1.0);
        }

        /// <summary>
        /// The get bytes.
        /// </summary>
        /// <param name="data">
        /// The data.
        /// </param>
        public override void GetBytes(byte[] data)
        {
            this.rng.GetBytes(data);
        }

        /// <summary>
        /// The get non zero bytes.
        /// </summary>
        /// <param name="data">
        /// The data.
        /// </param>
        public override void GetNonZeroBytes(byte[] data)
        {
            this.rng.GetNonZeroBytes(data);
        }

        /// <summary>
        /// The get unique key.
        /// </summary>
        /// <param name="maxSize">
        /// The max size.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>

    }
    /// <summary>
    /// The coupon code builder.
    /// </summary>
    public class CouponCodeBuilder
    {
        /// <summary>
        /// The symbols dictionary.
        /// </summary>
        private readonly Dictionary<char, int> symbolsDictionary = new Dictionary<char, int>();

        /// <summary>
        /// The random number generator
        /// </summary>
        private readonly RandomNumberGenerator randomNumberGenerator;

        /// <summary>
        /// The symbols array.
        /// </summary>
        private char[] symbols;

        /// <summary>
        /// Initializes a new instance of the <see cref="CouponCodeBuilder"/> class.
        /// </summary>
        public CouponCodeBuilder()
        {
            this.BadWordsList = new List<string>("SHPX PHAG JNAX JNAT CVFF PBPX FUVG GJNG GVGF SNEG URYY ZHSS QVPX XABO NEFR FUNT GBFF FYHG GHEQ FYNT PENC CBBC OHGG SRPX OBBO WVFZ WVMM CUNG'".Split(' '));
            this.SetupSymbolsDictionary();
            this.randomNumberGenerator = new SecureRandom();
        }

        /// <summary>
        /// Gets or sets the bad words list.
        /// </summary>
        public List<string> BadWordsList { get; set; }

        /// <summary>
        /// Define a delegate for your bad words list
        /// </summary>
        public Func<List<string>> SetBadWordsList { get; set; }

        /// <summary>
        /// The generate.
        /// </summary>
        /// <param name="opts">
        /// The opts.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public string Generate(Options opts)
        {
            var parts = new List<string>();

            // populate the bad words list with this delegate if it was set;
            if (this.SetBadWordsList != null)
            {
                this.BadWordsList = this.SetBadWordsList.Invoke();
            }

            // remove empty strings from list
            this.BadWordsList = this.BadWordsList.Except(new List<string> { string.Empty }).ToList();

            // if  plaintext wasn't set then override
            if (string.IsNullOrEmpty(opts.Plaintext))
            {
                // not yet implemented
                opts.Plaintext = this.GetRandomPlaintext(8);
            }

            // generate parts and combine
            do
            {
                // added to avoid infinite loop in case bad word is found
                parts.Clear();

                for (var i = 0; i < opts.Parts; i++)
                {
                    var sb = new StringBuilder();
                    for (var j = 0; j < opts.PartLength - 1; j++)
                    {
                        sb.Append(this.GetRandomSymbol());
                    }

                    var part = sb.ToString();
                    sb.Append(this.CheckDigitAlg1(part, i + 1));
                    parts.Add(sb.ToString());
                }
            }
            while (this.ContainsBadWord(string.Join(string.Empty, parts.ToArray())));

            return string.Join("-", parts.ToArray());
        }

        /// <summary>
        /// The validate.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="opts">The opts.</param>
        /// <returns>
        /// The <see cref="string" />.
        /// </returns>
        /// <exception cref="System.Exception">Provide a code to be validated</exception>
        /// <exception cref="Exception"></exception>
        public string Validate(string code, Options opts)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new Exception("Provide a code to be validated");
            }

            // uppercase the code, replace OIZS with 0125
            code = new string(Array.FindAll(code.ToCharArray(), char.IsLetterOrDigit))
                .ToUpper()
                .Replace("O", "0")
                .Replace("I", "1")
                .Replace("Z", "2")
                .Replace("S", "5");

            // split in the different parts
            var parts = new List<string>();
            var tmp = code;
            while (tmp.Length > 0)
            {
                parts.Add(tmp.Substring(0, opts.PartLength));
                tmp = tmp.Substring(opts.PartLength);
            }

            // make sure we have been given the same number of parts as we are expecting
            if (parts.Count != opts.Parts)
            {
                return string.Empty;
            }

            // validate each part
            for (var i = 0; i < parts.Count; i++)
            {
                var part = parts[i];

                // check this part has 4 chars
                if (part.Length != opts.PartLength)
                {
                    return string.Empty;
                }

                // split out the data and the check
                var data = part.Substring(0, opts.PartLength - 1);
                var check = part.Substring(opts.PartLength - 1, 1);

                if (Convert.ToChar(check) != this.CheckDigitAlg1(data, i + 1))
                {
                    return string.Empty;
                }
            }

            // everything looked ok with this code
            return string.Join("-", parts.ToArray());
        }

        /// <summary>
        /// The get random plaintext.
        /// </summary>
        /// <param name="maxSize">
        /// The max Size.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private string GetRandomPlaintext(int maxSize)
        {
            var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            var data = new byte[1];
            this.randomNumberGenerator.GetNonZeroBytes(data);

            data = new byte[maxSize];
            this.randomNumberGenerator.GetNonZeroBytes(data);

            var result = new StringBuilder(maxSize);
            foreach (var b in data)
            {
                result.Append(chars[b % chars.Length]);
            }

            return result.ToString();
        }

        /// <summary>
        /// The get random symbol.
        /// </summary>
        /// <returns>
        /// The <see cref="char"/>.
        /// </returns>
        private char GetRandomSymbol()
        {
            var rng = new SecureRandom();
            var pos = rng.Next(this.symbols.Length);
            return this.symbols[pos];
        }

        /// <summary>
        /// The check digit algorithm 1.
        /// </summary>
        /// <param name="data">
        /// The data.
        /// </param>
        /// <param name="check">
        /// The check.
        /// </param>
        /// <returns>
        /// The <see cref="char"/>.
        /// </returns>
        private char CheckDigitAlg1(string data, long check)
        {
            // check's initial value is the part number (e.g. 3 or above)
            // loop through the data chars
            Array.ForEach(
                data.ToCharArray(),
                v =>
                {
                    var k = this.symbolsDictionary[v];
                    check = (check * 19) + k;
                });

            return this.symbols[check % (this.symbols.Length - 1)];
        }

        /// <summary>
        /// The contains bad word.
        /// </summary>
        /// <param name="code">
        /// The code.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool ContainsBadWord(string code)
        {
            return this.BadWordsList
                .Except(new List<string> { string.Empty })
                .Any(t => code.ToUpper().IndexOf(t, StringComparison.Ordinal) > -1);
        }

        /// <summary>
        /// The setup of the symbols dictionary.
        /// </summary>
        private void SetupSymbolsDictionary()
        {
            const string AvailableSymbols = "0123456789ABCDEFGHJKLMNPQRTUVWXY";
            this.symbols = AvailableSymbols.ToCharArray();
            for (var i = 0; i < this.symbols.Length; i++)
            {
                this.symbolsDictionary.Add(this.symbols[i], i);
            }
        }
    }
}