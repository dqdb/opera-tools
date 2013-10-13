#include "stdafx.h"

#pragma comment(lib, "shlwapi")
#pragma comment(lib, "crypt32")

#define FILENAME_INSTALLER_PREFS_JSON				L"installer_prefs.json"
#define FILENAME_INSTALLATION_STATUS				L"installation_status.xml"
#define FILENAME_LOGIN_DATA							L"Login Data"
#define FILENAME_PREFERENCES						L"Preferences"
#define FILENAME_LAUNCHER_EXE						L"launcher.exe"
#define FILENAME_LOCKFILE							L"lockfile"
#define EXTENSION_JOURNAL							L"-journal"
#define EXTENSION_TEMP								L".temp"
#define EXTENSION_BACKUP							L".backup"
#define FOLDER_PASSWORDS							L"PortablePasswords"

#define ERROR_UNABLE_TO_FIND_LAUNCHER_EXE			1
#define ERROR_UNABLE_TO_FIND_OPERA_PROFILE			2
#define ERROR_INVALID_OPERA_PROFILE					3
#define ERROR_UNABLE_TO_CREATE_PASSWORDS_FOLDER		4
#define ERROR_UNABLE_TO_ENCRYPT_PASSWORDS			5
#define ERROR_UNABLE_TO_DECRYPT_PASSWORDS			6
#define ERROR_UNABLE_TO_RUN_LAUNCHER_EXE			7

char * LoadFileIntoString(PCWSTR pwszFileName)
{
	HANDLE handle = CreateFile(pwszFileName, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
	char * pszResult = NULL;
	if (handle != INVALID_HANDLE_VALUE)
	{
		DWORD cbFile = GetFileSize(handle, NULL);
		if (cbFile != INVALID_FILE_SIZE)
		{
			pszResult = (char *)malloc(cbFile + 1);
			if (pszResult != NULL)
			{
				pszResult[cbFile] = 0;
				DWORD cbRead;
				if (!ReadFile(handle, pszResult, cbFile, &cbRead, NULL) || cbRead != cbFile)
				{
					free(pszResult);
					pszResult = NULL;
				}
			}
		}

		CloseHandle(handle);
	}

	return pszResult;
}

// not the nicest solution, but it does not require a complete JSON reader library
bool IsOperaPortable()
{
	char * pszInstallerPrefsJson = LoadFileIntoString(FILENAME_INSTALLER_PREFS_JSON);
	if (pszInstallerPrefsJson == NULL)
		return false;

	bool fIsPortable = strstr(pszInstallerPrefsJson, "\"single_profile\": true") != NULL;
	free(pszInstallerPrefsJson);
	return fIsPortable;
}

// not the nicest solution, but it does not require using MSXML or other XML library
PCWSTR GetOperaChannel()
{
	char * pszInstallationStatusXml = LoadFileIntoString(FILENAME_INSTALLATION_STATUS);
	if (pszInstallationStatusXml == NULL)
		return NULL;

	PCWSTR pwszChannel = NULL;
	if (strstr(pszInstallationStatusXml, "Software\\Classes\\OperaDeveloper") != NULL)
		pwszChannel = L"Opera Developer";
	else if (strstr(pszInstallationStatusXml, "Software\\Classes\\OperaNext") != NULL)
		pwszChannel = L"Opera Next";
	else if (strstr(pszInstallationStatusXml, "Software\\Classes\\OperaStable") != NULL)
		pwszChannel = L"Opera Stable";
	free(pszInstallationStatusXml);
	return pwszChannel;
}

// safe version of the API function
PWSTR PathAddBackslash_s(PWSTR pwszPath, int cchPath)
{
	int cch = wcslen(pwszPath);
	if (cch <= 0 || (pwszPath[cch - 1] == '\\' || pwszPath[cch - 1] == '/') || cch >= cchPath - 1)
		return pwszPath + cch;

	pwszPath[cch] = '\\';
	pwszPath[cch + 1] = 0;
	return pwszPath + cch + 1;
}

// safe version of the API function
void PathAppend_s(PWSTR pwszPath, int cchPath, PCWSTR pwszMore)
{
	PWSTR pwszPath1 = PathAddBackslash_s(pwszPath, cchPath);
	wcscpy_s(pwszPath1, cchPath - (pwszPath1 - pwszPath), pwszMore);
}

// safe version of the API function
PCWSTR PathCombine_s(PWSTR pwszPathOut, int cchPathOut, PCWSTR pwszPathIn, PCWSTR pwszMore)
{
	wcscpy_s(pwszPathOut, cchPathOut, pwszPathIn);
	PathAppend_s(pwszPathOut, cchPathOut, pwszMore);
	return pwszPathOut;
}

bool GetOperaProfileFolder(PWSTR pwsz, int cch)
{
	if (IsOperaPortable())
	{
		GetCurrentDirectory(cch, pwsz);
		PathAppend_s(pwsz, cch, L"profile\\data");
		return true;
	}

	if (cch < MAX_PATH)
		return false;

	PCWSTR pwszChannel = GetOperaChannel();
	if (pwszChannel == NULL)
		return false;

	if (FAILED(SHGetFolderPath(NULL, CSIDL_APPDATA, NULL, SHGFP_TYPE_CURRENT, pwsz)))
		return false;

	PathAppend_s(pwsz, cch, L"Opera Software");
	PathAppend_s(pwsz, cch, pwszChannel);
	return true;
}

HRESULT ProcessPasswords(PCWSTR pwszSourceFolder, PCWSTR pwszTargetFolder, bool fDecrypt)
{
	WCHAR wszSourceLoginData[MAX_PATH];
	WCHAR wszSourceLoginDataJournal[MAX_PATH];
	WCHAR wszBackupLoginData[MAX_PATH];
	WCHAR wszBackupLoginDataJournal[MAX_PATH];
	WCHAR wszTargetLoginData[MAX_PATH];
	WCHAR wszTargetLoginDataJournal[MAX_PATH];
	WCHAR wszTempLoginData[MAX_PATH];
	WCHAR wszTempLoginDataJournal[MAX_PATH];
	char szTempLoginData[MAX_PATH];

	PathCombine_s(wszSourceLoginData, MAX_PATH, pwszSourceFolder, FILENAME_LOGIN_DATA);
	PathCombine_s(wszSourceLoginDataJournal, MAX_PATH, pwszSourceFolder, FILENAME_LOGIN_DATA EXTENSION_JOURNAL);

	PathCombine_s(wszTargetLoginData, MAX_PATH, pwszTargetFolder, FILENAME_LOGIN_DATA);
	PathCombine_s(wszTargetLoginDataJournal, MAX_PATH, pwszTargetFolder, FILENAME_LOGIN_DATA EXTENSION_JOURNAL);

	PathCombine_s(wszTempLoginData, MAX_PATH, pwszTargetFolder, FILENAME_LOGIN_DATA EXTENSION_TEMP);
	PathCombine_s(wszTempLoginDataJournal, MAX_PATH, pwszTargetFolder, FILENAME_LOGIN_DATA EXTENSION_TEMP EXTENSION_JOURNAL);

	PathCombine_s(wszBackupLoginData, MAX_PATH, pwszTargetFolder, FILENAME_LOGIN_DATA EXTENSION_BACKUP);
	PathCombine_s(wszBackupLoginDataJournal, MAX_PATH, pwszTargetFolder, FILENAME_LOGIN_DATA EXTENSION_BACKUP EXTENSION_JOURNAL);

	DeleteFile(wszTempLoginData);
	DeleteFile(wszTempLoginDataJournal);
	if (!PathFileExists(wszSourceLoginData) || !PathFileExists(wszSourceLoginDataJournal))
		return S_OK; // nothing to do

	if (!CopyFile(wszSourceLoginData, wszTempLoginData, TRUE) || 
		!CopyFile(wszSourceLoginDataJournal, wszTempLoginDataJournal, TRUE) ||
		!WideCharToMultiByte(CP_ACP, 0, wszTempLoginData, MAX_PATH, szTempLoginData, MAX_PATH, NULL, NULL))
		return HRESULT_FROM_WIN32(GetLastError());

	sqlite3 * pDatabase = NULL;
	sqlite3_stmt * pJournal = NULL;
	sqlite3_stmt * pSelect = NULL;
	sqlite3_stmt * pUpdate = NULL;
	sqlite3_stmt * pBeginTransaction = NULL;
	sqlite3_stmt * pCommit = NULL;
	int nResult = sqlite3_open(szTempLoginData, &pDatabase);
	if (nResult == SQLITE_OK)
		nResult = sqlite3_prepare(pDatabase, "PRAGMA journal_mode = PERSIST", -1, &pJournal, NULL);
	if (nResult == SQLITE_OK)
		nResult = sqlite3_prepare(pDatabase, "BEGIN TRANSACTION", -1, &pBeginTransaction, NULL);
	if (nResult == SQLITE_OK)
		nResult = sqlite3_prepare(pDatabase, "COMMIT", -1, &pCommit, NULL);
	if (nResult == SQLITE_OK)
		nResult = sqlite3_prepare(pDatabase, "SELECT origin_url, username_element, username_value, password_element, submit_element, signon_realm, password_value FROM logins", -1, &pSelect, NULL);
	if (nResult == SQLITE_OK)
		nResult = sqlite3_prepare(pDatabase, "UPDATE logins SET password_value = ? WHERE origin_url = ? AND username_element = ? AND username_value = ? AND password_element = ? AND submit_element = ? AND signon_realm = ?", -1, &pUpdate, NULL);

	if (nResult == SQLITE_OK)
	{
		nResult = sqlite3_step(pJournal);
		if (nResult == SQLITE_ROW)
			nResult = sqlite3_step(pBeginTransaction);
		if (nResult == SQLITE_DONE)
			nResult = SQLITE_OK;
	}

	if (nResult == SQLITE_OK)
	{
		for (;;)
		{
			nResult = sqlite3_step(pSelect);
			if (nResult != SQLITE_ROW)
				break;

			DATA_BLOB input;
			DATA_BLOB output = {0};

			// pbData should not be NULL even for empty blobs
			input.cbData = sqlite3_column_bytes(pSelect, 6);
			input.pbData = (BYTE *)(input.cbData ? sqlite3_column_blob(pSelect, 6) : "");

			if ((fDecrypt && input.cbData > 0 && input.pbData[0] != 0x01) ||
				(!fDecrypt && input.cbData > 0 && input.pbData[0] == 0x01))
				continue; // nothing to do because password is already encrypted or decrypted

			if ((fDecrypt && !CryptUnprotectData(&input, NULL, NULL, NULL, NULL, 0, &output)) ||
				(!fDecrypt && !CryptProtectData(&input, NULL, NULL, NULL, NULL, 0, &output)))
				continue; // skip password entry because unable to encrypt or decrypt the password

			nResult = sqlite3_reset(pUpdate);
			if (nResult != SQLITE_OK)
				break;

			sqlite3_bind_blob(pUpdate, 1, output.pbData, output.cbData, SQLITE_TRANSIENT);
			LocalFree(output.pbData);
			for (int n = 0; n < 6; n++)
				sqlite3_bind_text(pUpdate, n + 2, (const char *)sqlite3_column_text(pSelect, n), -1, SQLITE_TRANSIENT);
			nResult = sqlite3_step(pUpdate);
			if (nResult != SQLITE_DONE)
				break;
		}
	}

	if (nResult == SQLITE_OK || nResult == SQLITE_DONE)
		nResult = sqlite3_step(pCommit);

	if (pSelect != NULL)
		sqlite3_finalize(pSelect);
	if (pUpdate != NULL)
		sqlite3_finalize(pUpdate);
	if (pJournal != NULL)
		sqlite3_finalize(pJournal);
	if (pBeginTransaction != NULL)
		sqlite3_finalize(pBeginTransaction);
	if (pCommit != NULL)
		sqlite3_finalize(pCommit);
	if (pDatabase != NULL)
		sqlite3_close(pDatabase);

	if (nResult != SQLITE_OK && nResult != SQLITE_DONE)
		return nResult | 0x81aa0000;

	DeleteFile(wszBackupLoginData);
	DeleteFile(wszBackupLoginDataJournal);
	if ((!MoveFile(wszTargetLoginData, wszBackupLoginData) && GetLastError() != ERROR_FILE_NOT_FOUND) || 
		(!MoveFile(wszTargetLoginDataJournal, wszBackupLoginDataJournal) && GetLastError() != ERROR_FILE_NOT_FOUND) || 
		!MoveFile(wszTempLoginData, wszTargetLoginData) || 
		!MoveFile(wszTempLoginDataJournal, wszTargetLoginDataJournal))
		return HRESULT_FROM_WIN32(GetLastError());

	return S_OK;
}

void WaitOperaToStop(PROCESS_INFORMATION * ppi)
{
	// wait launcher.exe to exit
	WaitForSingleObject(ppi->hProcess, INFINITE);

	// find main Opera process and wait to exit
	HANDLE handleProcesses = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
	if (handleProcesses == INVALID_HANDLE_VALUE)
		return;

	PROCESSENTRY32 pe;
	HANDLE handleOpera = NULL;
	pe.dwSize = sizeof(pe);
	if (!Process32First(handleProcesses, &pe))
	{
		CloseHandle(handleProcesses);
		return;
	}

	do
	{
		if (pe.th32ParentProcessID == ppi->dwProcessId)
		{
			handleOpera = OpenProcess(SYNCHRONIZE, FALSE, pe.th32ProcessID);
			break;
		}
	}
	while (Process32Next(handleProcesses, &pe));

	CloseHandle(handleProcesses);
	if (handleOpera == NULL)
		return;

	WaitForSingleObject(handleOpera, INFINITE);
	CloseHandle(handleOpera);
}

int Error(int nCode, PCWSTR pwszFormat, ...)
{
	WCHAR wszText[1024];
	va_list args;
	va_start(args, pwszFormat);
	vswprintf_s(wszText, 1024, pwszFormat, args);
	va_end(args);
	MessageBox(GetActiveWindow(), wszText, L"Error", MB_OK | MB_ICONERROR);
	return nCode;
}

int APIENTRY _tWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPTSTR lpCmdLine, int nCmdShow)
{
	WCHAR wszLauncherExe[MAX_PATH];
	WCHAR wszProfileFolder[MAX_PATH];
	WCHAR wszPasswordsFolder[MAX_PATH];
	WCHAR wszLockFile[MAX_PATH];
	WCHAR wszPreferences[MAX_PATH];

	GetCurrentDirectory(MAX_PATH, wszLauncherExe);
	PathAppend_s(wszLauncherExe, MAX_PATH, FILENAME_LAUNCHER_EXE);
	if (!PathFileExists(wszLauncherExe))
		return Error(ERROR_UNABLE_TO_FIND_LAUNCHER_EXE, L"Unable to find Opera executable.");

	if (!GetOperaProfileFolder(wszProfileFolder, MAX_PATH))
		return Error(ERROR_UNABLE_TO_FIND_OPERA_PROFILE, L"Unable to find Opera profile folder.");

	PathCombine_s(wszLockFile, MAX_PATH, wszProfileFolder, FILENAME_LOCKFILE);
	if (PathFileExists(wszLockFile))
		return 0;

	PathCombine_s(wszPreferences, MAX_PATH, wszProfileFolder, FILENAME_PREFERENCES);
	if (!PathFileExists(wszPreferences))
		return Error(ERROR_INVALID_OPERA_PROFILE, L"Invalid or corrupted Opera profile folder.");

	PathCombine_s(wszPasswordsFolder, MAX_PATH, wszProfileFolder, FOLDER_PASSWORDS);
	if (!CreateDirectory(wszPasswordsFolder, NULL) && GetLastError() != ERROR_ALREADY_EXISTS)
		return Error(ERROR_UNABLE_TO_CREATE_PASSWORDS_FOLDER, L"Unable to create folder \"Passwords\" [%d].", GetLastError());

	HRESULT hr = ProcessPasswords(wszPasswordsFolder, wszProfileFolder, false);
	if (FAILED(hr))
		Error(ERROR_UNABLE_TO_ENCRYPT_PASSWORDS, L"Unable to process passwords before running Opera [%08x].", hr);

	PROCESS_INFORMATION pi;
	STARTUPINFO si = { sizeof(si) };
	if (!CreateProcess(wszLauncherExe, lpCmdLine, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi))
		return Error(ERROR_UNABLE_TO_RUN_LAUNCHER_EXE, L"Unable to run Opera executable [%d].", GetLastError());

	WaitOperaToStop(&pi);
	CloseHandle(pi.hProcess);
	CloseHandle(pi.hThread);

	hr = ProcessPasswords(wszProfileFolder, wszPasswordsFolder, true);
	if (FAILED(hr))
		Error(ERROR_UNABLE_TO_DECRYPT_PASSWORDS, L"Unable to process passwords after Opera exited [%08x].", hr);

	return 0;
}
