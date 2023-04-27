// Program.cs

using System.Runtime.InteropServices;
using COM;
using CoreWidgetProvider.Widgets;

[DllImport("ole32.dll")]

static extern int CoRegisterClassObject(
            [MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
            [MarshalAs(UnmanagedType.IUnknown)] object pUnk,
            uint dwClsContext,
            uint flags,
            out uint lpdwRegister);

[DllImport("ole32.dll")]
static extern int CoRevokeClassObject(uint dwRegister);

Console.WriteLine("Registering Widget Provider");

uint cookie;

#pragma warning disable SA1312 // Variable names should begin with lower-case letter
Guid CLSID_Factory = Guid.Parse("F8B2DBB9-3687-4C6E-99B2-B92C82905937");
#pragma warning restore SA1312 // Variable names should begin with lower-case letter

#pragma warning disable SA1312 // Variable names should begin with lower-case letter
var _ = CoRegisterClassObject(CLSID_Factory, new WidgetProviderFactory<WidgetProvider>(), 0x4, 0x1, out cookie);
#pragma warning restore SA1312 // Variable names should begin with lower-case letter

Console.WriteLine("Registered successfully. Press ENTER to exit.");

Console.ReadLine();

_ = CoRevokeClassObject(cookie);
