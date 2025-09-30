// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using SDL;

namespace Silk.NET.Windowing.Sdl
{
    internal class SdlVkSurface : IVkSurface
    {
        private readonly SdlView _view;

        public SdlVkSurface(SdlView view)
        {
            _view = view;
        }

        public unsafe VkNonDispatchableHandle Create<T>(VkHandle instance, T* allocator) where T : unmanaged
        {
            VkNonDispatchableHandle ret;
            var ret2 = (VkSurfaceKHR_T*)0;
            if (SDL3.SDL_Vulkan_CreateSurface(_view.SdlWindow, (VkInstance_T*)instance.Handle, null, &ret2) == false)
            {
                throw new PlatformException("Failed to create surface.", SdlExt.GetErrorAsException());
            }
            ret.Handle = (ulong)ret2;

            return ret;
        }

        private unsafe byte** _requiredExtensions;

        public unsafe byte** GetRequiredExtensions(out uint count)
        {
            fixed (uint* countPtr = &count) {
                return SDL3.SDL_Vulkan_GetInstanceExtensions(countPtr);
            }
        }
    }
}
