/*
 * Copyright (c) 2021 JSC TBC Bank
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace TBC.OpenBanking.Jws
{
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using JavaScriptEncoder = System.Text.Encodings.Web.JavaScriptEncoder;

    static internal class Helper
    {
        private static readonly JsonSerializerOptions Options;

        static Helper()
        {
            // These options match Newtonsoft.Json defaults, more or less.

            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                AllowTrailingCommas         = true,
                DefaultBufferSize           = 81920,  // Keeping under large object heap treshold (85K)
                MaxDepth                    = 128,  // Newtonsoft has no limit on this
                DictionaryKeyPolicy         = JsonNamingPolicy.CamelCase,
                Encoder                     = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                IncludeFields               = false,
                NumberHandling              = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
                ReadCommentHandling         = JsonCommentHandling.Skip,
                WriteIndented               = false,
            };

            Options = options;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal string SerializeToJson(object obj) =>
            JsonSerializer.Serialize(obj, options: Options);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal string SerializeToJson<T>(T obj) =>
            JsonSerializer.Serialize<T>(obj, options: Options);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal T DeserializeFromJson<T>(string jsonString) =>
            JsonSerializer.Deserialize<T>(jsonString, options: Options);
    }
}
