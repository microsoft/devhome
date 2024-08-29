The tool is used to populate the error lookup database.

The HRESULTs, Win32 error codes, and NT Statuses are sourced from here

https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/1bc92ddf-b79e-413c-bbaa-99a5281a6c90

The version of the protocol used is 22.0.

The error codes were copied from the above document and pasted into HRESULT.txt, WIN32.txt, and NTStatus.txt (by hand) for the tool to process.

Note, the documentation resuses multiple error codes for different numbers, so error codes cannot be unique identifiers

To update the DB, update the HRESULTS.txt, WIN32.txt, and NTSTATUS.txt files and run the tool. Then copy the generated errors.db to the tools\WindowsPI\WindowsPI directory. Then run

"sqlite3 errors.db .dump > errors.db.txt"

to update the text version of the database.




