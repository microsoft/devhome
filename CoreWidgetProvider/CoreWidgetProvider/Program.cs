// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Runtime.InteropServices;
using COM;
using CoreWidgetProvider.Widgets;
using Windows.Win32;
using Windows.Win32.System.Com;

Console.WriteLine("Registering Widget Provider");

uint cookie;

#pragma warning disable SA1312 // Variable names should begin with lower-case letter
Guid CLSID_Factory = Guid.Parse("F8B2DBB9-3687-4C6E-99B2-B92C82905937");
#pragma warning restore SA1312 // Variable names should begin with lower-case letter

#pragma warning disable SA1312 // Variable names should begin with lower-case letter
var _ = PInvoke.CoRegisterClassObject(CLSID_Factory, new WidgetProviderFactory<WidgetProvider>(), CLSCTX.CLSCTX_LOCAL_SERVER, (REGCLS)0x1, out cookie);
#pragma warning restore SA1312 // Variable names should begin with lower-case letter

Console.WriteLine("Registered successfully. Press ENTER to exit.");

Console.ReadLine();

_ = PInvoke.CoRevokeClassObject(cookie);
