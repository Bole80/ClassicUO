// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SetRasterizerStateCommand
    {
        public int type;

        public IntPtr id;
    }
}