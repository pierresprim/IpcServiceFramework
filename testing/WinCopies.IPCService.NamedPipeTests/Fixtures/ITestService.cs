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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;

namespace WinCopies.IPCService.NamedPipeTests.Fixtures
{
    public interface ITestService
    {
        int PrimitiveTypes(bool a, byte b, sbyte c, char d, decimal e, double f, float g, int h, uint i, long j,
            ulong k, short l, ushort m);
        string StringType(string input);
        Complex ComplexType(Complex input);
        IEnumerable<Complex> ComplexTypeArray(IEnumerable<Complex> input);
        void ReturnVoid();
        DateTime DateTime(DateTime input);
        DateTimeStyles EnumType(DateTimeStyles input);
        byte[] ByteArray(byte[] input);
        T GenericMethod<T>(T input);
        Task<int> AsyncMethod();
        void ThrowException();
        ITestDto Abstraction(ITestDto input);
        void UnserializableInput(UnserializableObject input);
        UnserializableObject UnserializableOutput();
    }

    public interface ITestService2
    {
        int SomeMethod();
    }
}
