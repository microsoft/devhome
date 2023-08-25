// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;

namespace DevHome.Common.Services;
public interface IAccessibilityService
{
    public event EventHandler<string>? AnnoucementTextChanged;

    public void Annouce(string text);
}
