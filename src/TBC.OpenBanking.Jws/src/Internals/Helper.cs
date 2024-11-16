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

namespace TBC.OpenBanking.Jws;

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using JavaScriptEncoder = System.Text.Encodings.Web.JavaScriptEncoder;

internal static class Helper
{
    // These options match Newtonsoft.Json's defaults, more or less
    private static readonly JsonSerializerOptions s_options = new(JsonSerializerDefaults.Web)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string SerializeToJson(object obj) => JsonSerializer.Serialize(obj, s_options);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string SerializeToJson<T>(T obj) => JsonSerializer.Serialize<T>(obj, s_options);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T DeserializeFromJson<T>(string jsonString) => JsonSerializer.Deserialize<T>(jsonString, s_options);
}
