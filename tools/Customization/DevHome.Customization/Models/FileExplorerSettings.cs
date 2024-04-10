// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace DevHome.Customization.Models;

internal static class FileExplorerSettings
{
    public static bool ShowFileExtensionsEnabled()
    {
        return GetShellSettings(SSF_MASK.SSF_SHOWEXTENSIONS);
    }

    public static void SetShowFileExtensionsEnabled(bool value)
    {
        SetShellSettings(SSF_MASK.SSF_SHOWEXTENSIONS, value);
    }

    public static bool ShowHiddenAndSystemFilesEnabled()
    {
        return GetShellSettings(SSF_MASK.SSF_SHOWALLOBJECTS);
    }

    public static void SetShowHiddenAndSystemFilesEnabled(bool value)
    {
        SetShellSettings(SSF_MASK.SSF_SHOWALLOBJECTS, value);
    }

    public static bool ShowFullPathInTitleBarEnabled()
    {
        return GetCabineteState(CabinetStateFlags.FullPathTitle);
    }

    public static void SetShowFullPathInTitleBarEnabled(bool value)
    {
        SetCabinetState(CabinetStateFlags.FullPathTitle, value);
    }

    private static bool GetShellSettings(SSF_MASK mask)
    {
        unsafe
        {
            var state = default(SHELLSTATEA);
            PInvoke.SHGetSetSettings(&state, mask, false);
            return (state._bitfield1 & (int)mask) != 0;
        }
    }

    private static void SetShellSettings(SSF_MASK mask, bool value)
    {
        unsafe
        {
            var state = default(SHELLSTATEA);
            PInvoke.SHGetSetSettings(&state, mask, false);
            if (value)
            {
                state._bitfield1 |= (int)mask;
            }
            else
            {
                state._bitfield1 &= ~(int)mask;
            }

            PInvoke.SHGetSetSettings(&state, mask, true);
        }
    }

    [Flags]
    private enum CabinetStateFlags
    {
        FullPathTitle = 0x00000001,
    }

    private static bool GetCabineteState(CabinetStateFlags flags)
    {
        unsafe
        {
            var state = default(CABINETSTATE);
            PInvoke.ReadCabinetState(&state, sizeof(CABINETSTATE));
            return (state._bitfield & (int)flags) != 0;
        }
    }

    private static void SetCabinetState(CabinetStateFlags flags, bool value)
    {
        unsafe
        {
            var state = default(CABINETSTATE);
            PInvoke.ReadCabinetState(&state, sizeof(CABINETSTATE));
            if (value)
            {
                state._bitfield |= (int)flags;
            }
            else
            {
                state._bitfield &= ~(int)flags;
            }

            PInvoke.WriteCabinetState(&state);
        }
    }
}
