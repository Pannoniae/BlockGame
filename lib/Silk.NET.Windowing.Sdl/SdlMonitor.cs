// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Silk.NET.Maths;
using SDL;

namespace Silk.NET.Windowing.Sdl
{
    internal struct SdlMonitor : IMonitor
    {
        private readonly SdlPlatform _platform;

        public SdlMonitor(SdlPlatform platform, SDL_DisplayID i)
        {
            _platform = platform;
            Index = (int)i;
        }

        public IWindow CreateWindow(WindowOptions opts) => new SdlWindow(opts, null, this, _platform);

        public unsafe string Name
        {
            get
            {
                var namePtr = SDL3.SDL_GetDisplayName((SDL_DisplayID)Index);
                return namePtr ?? "";
            }
        }

        public int Index { get; }

        public unsafe Rectangle<int> Bounds
        {
            get
            {
                SDL_Rect rect;
                SDL3.SDL_GetDisplayUsableBounds((SDL_DisplayID)Index, &rect);
                return new Rectangle<int>(rect.x, rect.y, rect.w, rect.h);
            }
        }

        public unsafe VideoMode VideoMode
        {
            get
            {
                var modePtr = SDL3.SDL_GetCurrentDisplayMode((SDL_DisplayID)Index);
                if (modePtr == null) return default;
                return new VideoMode(new Vector2D<int>(modePtr->w, modePtr->h), (int)modePtr->refresh_rate);
            }
        }

        public float Gamma
        {
            get => 1;
            set { }
        }

        public unsafe IEnumerable<VideoMode> GetAllVideoModes()
        {
            int count;
            var modesPtr = SDL3.SDL_GetFullscreenDisplayModes((SDL_DisplayID)Index, &count);
            var ret = new VideoMode[count];
            for (var i = 0; i < count; i++)
            {
                var mode = modesPtr[i];
                ret[i] = new VideoMode(new Vector2D<int>(mode->w, mode->h), (int)mode->refresh_rate);
            }

            return ret;
        }
    }
}
