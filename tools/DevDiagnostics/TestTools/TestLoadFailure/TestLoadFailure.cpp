// TestLoadFailure.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <windows.h>

int main()
{
    if (NULL == LoadLibrary(L"MissingFile.dll"))
    {
        return GetLastError();
    }

    return 0;
}
