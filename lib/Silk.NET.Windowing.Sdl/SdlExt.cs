using SDL;
using Silk.NET.SDL;

namespace Silk.NET.Windowing.Sdl;

public static class SdlExt {
    public static void ThrowError()
    {
        var ex = GetErrorAsException();
        if (!(ex is null))
        {
            throw ex;
        }
    }

    public static SdlException? GetErrorAsException()
    {
        var str = SDL3.SDL_GetError();
        if (string.IsNullOrWhiteSpace(str))
        {
            return null;
        }

        SDL3.SDL_ClearError();
        return new SdlException(str);
    }
}