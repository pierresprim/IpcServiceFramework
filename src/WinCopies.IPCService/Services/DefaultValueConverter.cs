/* MIT License

Copyright (c) 2018 Jacques Kang Copyright (c) 2021 Pierre Sprimont

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Globalization;
using WinCopies.Util;

namespace WinCopies.IPCService.Services
{
    public class DefaultValueConverter : IValueConverter
    {
        public bool TryConvert(object origValue, Type destType, out object destValue)
        {
            if (destType is null)

                throw new ArgumentNullException(nameof(destType));

            Type destConcreteType = Nullable.GetUnderlyingType(destType);

            if (origValue == null)
            {
                destValue = null;

                return destType.IsClass || (destConcreteType != null);
            }

            if (destConcreteType != null)

                destType = destConcreteType;

            if (destType.IsAssignableFrom(origValue.GetType()))
            {
                // copy value directly if it can be assigned to destType
                destValue = origValue;

                return true;
            }

            bool catchException(in System.Exception ex) => ex.Is(false, typeof(ArgumentNullException), typeof(ArgumentException));

            if (destType.IsEnum)

                if (origValue is string str)

                    try
                    {
                        destValue = Enum.Parse(destType, str, ignoreCase: true);

                        return true;
                    }

                    catch (System.Exception ex) when (catchException(ex) ||
                    ex is OverflowException)
                    { }

                else
                {
                    try
                    {
                        destValue = Enum.ToObject(destType, origValue);

                        return true;
                    }

                    catch (System.Exception ex) when (catchException(ex))
                    { }
                }

            if (origValue is string origStringValue)
            {
                if ((destType == typeof(Guid)) && Guid.TryParse(origStringValue, out Guid guidResult))
                {
                    destValue = guidResult;

                    return true;
                }

                if ((destType == typeof(TimeSpan)) && TimeSpan.TryParse(origStringValue, CultureInfo.InvariantCulture, out TimeSpan timeSpanResult))
                {
                    destValue = timeSpanResult;

                    return true;
                }

                if ((destType == typeof(DateTime)) && DateTime.TryParse(origStringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTimeResult))
                {
                    destValue = dateTimeResult;

                    return true;
                }
            }

            if ((origValue is TimeSpan timeSpan) && (destType == typeof(string)))
            {
                destValue = timeSpan.ToString("c", CultureInfo.InvariantCulture);

                return true;
            }

            if ((origValue is DateTime dateTime) && (destType == typeof(string)))
            {
                destValue = dateTime.ToString("o", CultureInfo.InvariantCulture);

                return true;
            }

            if (origValue is JObject jObj)
            {
                // rely on JSON.Net to convert complexe type
                destValue = jObj.ToObject(destType);
                // TODO: handle error
                return true;
            }

            if (origValue is JArray jArray)
            {
                destValue = jArray.ToObject(destType);

                return true;
            }

            try
            {
                destValue = Convert.ChangeType(origValue, destType, CultureInfo.InvariantCulture);

                return true;
            }

            catch (System.Exception ex) when (ex.Is(false,
                typeof(InvalidCastException),
                typeof(FormatException),
                typeof(OverflowException),
                typeof(ArgumentNullException)))
            { }

            try
            {
                destValue = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(origValue), destType);

                return true;
            }
            catch (JsonException)
            { }

            destValue = null;

            return false;
        }
    }
}
