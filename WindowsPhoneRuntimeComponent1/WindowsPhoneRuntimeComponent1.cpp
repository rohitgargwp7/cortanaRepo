// WindowsPhoneRuntimeComponent1.cpp
#include "pch.h"
#include "WindowsPhoneRuntimeComponent1.h"
#include "zmedialib.h"
#include <vector>
#include <utility>
#include <collection.h> 
using namespace WindowsPhoneRuntimeComponent1;
using namespace Platform;
using namespace std;
using namespace Platform::Collections; 
using namespace Windows::Foundation::Collections; 
using namespace std;

WindowsPhoneRuntimeComponent::WindowsPhoneRuntimeComponent()
{
}

void WindowsPhoneRuntimeComponent::myfunc(uint8 position, Platform::WriteOnlyArray<uint8>^ intOutArray, Platform::String^* strVideoFilePath,Platform::String^* strVideoFilename) {
	int i = 0;

	auto_ZMediaLibRequirement myLib;

	myLib.Require();
	HZMEDIALIST* phList = NULL;
	ULONGLONG listSize = 0;
	ULONG pullCount = 0;

	HRESULT hr = 0;

	HZMEDIALIST hRootList = NULL;
	//hr = ZMediaLib_CreateList(ZMEDIALIST_TYPE_FOLDER_FOLDERS, ZMEDIAITEM_ROOTFOLDER, &hRootList);
	hr = ZMediaLib_CreateList(ZMEDIALIST_TYPE_ALL_VIDEOS, ZMEDIAITEM_ROOTFOLDER, &hRootList);

	size_t cItemsRoot = 0;
	hr = ZMediaList_GetItemCount(hRootList, &cItemsRoot);

	ZMEDIAITEM *rgItemsRoot = (ZMEDIAITEM*)malloc(sizeof(ZMEDIAITEM) * cItemsRoot);


	hr = ZMediaList_GetItems(hRootList, 0, rgItemsRoot, cItemsRoot, &cItemsRoot);

	// Find the folder with a name matching c_wszRootPictureFolder

	LPWSTR wszName = NULL;
	ZMEDIAITEM zmiPicturesFolder = NULL;

	size_t cch = 0;

	//Get the thumbnail
	hr = ZMediaLib_GetItemThumbnail(rgItemsRoot[position], ZMEDIAITEM_THUMBTYPE_NORMAL, NULL, 0, &cch);
	byte* myThumbData = (byte*) malloc(cch);// *sizeof(WCHAR));
	hr = ZMediaLib_GetItemThumbnail(rgItemsRoot[position], ZMEDIAITEM_THUMBTYPE_NORMAL, (void*)myThumbData, cch, NULL);

	//Now get the original asset
	ZMEDIAITEM *pItem = (ZMEDIAITEM*)malloc(sizeof(ZMEDIAITEM));
	hr = ZMediaList_GetItem(hRootList, 0, pItem);

	ZMEDIAITEMSTREAM *pStream = (ZMEDIAITEMSTREAM*)malloc(sizeof(ZMEDIAITEMSTREAM));
	hr = ZMediaLib_GetItemStreamOnProperty(*pItem, ZMEDIAITEM_ATTRIBUTE_FILEPATH, pStream);

	ULARGE_INTEGER *pSize = (ULARGE_INTEGER *)malloc(sizeof(ULARGE_INTEGER));
	hr = ZMediaLib_GetSizeItemStream(*pStream, pSize);



	auto data = ref new Platform::Array<uint8>(cch);

	for (int iter = 0; iter < cch; iter++)
	{
		intOutArray[iter] = (uint8) myThumbData[iter];
	}

	// get filename
	hr = ZMediaLib_GetItemStringAttribute(rgItemsRoot[position], ZMEDIAITEM_ATTRIBUTE_FILENAME, NULL, NULL, &cch);
	WCHAR *str = new WCHAR[cch];
	hr = ZMediaLib_GetItemStringAttribute(rgItemsRoot[position], ZMEDIAITEM_ATTRIBUTE_FILENAME, str, cch, &cch);
	*strVideoFilename=ref new String(str);
	delete str;
	hr = ZMediaLib_GetItemStringAttribute(rgItemsRoot[position], ZMEDIAITEM_ATTRIBUTE_FILEPATH, NULL, NULL, &cch);
	str = new WCHAR[cch]; 
	hr = ZMediaLib_GetItemStringAttribute(rgItemsRoot[position], ZMEDIAITEM_ATTRIBUTE_FILEPATH, str, cch, &cch);
	*strVideoFilePath = ref new String(str);

	//CREATEFILE2_EXTENDED_PARAMETERS extendedParams = { 0 };
	//extendedParams.dwSize = sizeof(CREATEFILE2_EXTENDED_PARAMETERS);
	//extendedParams.dwFileAttributes = FILE_ATTRIBUTE_NORMAL;
	//extendedParams.dwFileFlags = FILE_FLAG_SEQUENTIAL_SCAN;
	//extendedParams.dwSecurityQosFlags = SECURITY_ANONYMOUS;
	//extendedParams.lpSecurityAttributes = nullptr;
	//extendedParams.hTemplateFile = nullptr;

	//HANDLE hFile = CreateFile2(str, GENERIC_READ, FILE_SHARE_READ, OPEN_EXISTING, &extendedParams);

	//delete str;
	//str = new WCHAR[extendedParams.dwSize];
	//DWORD dNumberOfBytesRead = 0;
	//BOOL readSuccess = ReadFile(hFile, (void *) str, extendedParams.dwSize, &dNumberOfBytesRead, NULL);
}

uint16 WindowsPhoneRuntimeComponent::GetVideoCount()
{
	auto_ZMediaLibRequirement myLib;

	myLib.Require();
	HZMEDIALIST* phList = NULL;
	ULONGLONG listSize = 0;
	ULONG pullCount = 0;

	HRESULT hr = 0;

	HZMEDIALIST hRootList = NULL;
	//hr = ZMediaLib_CreateList(ZMEDIALIST_TYPE_FOLDER_FOLDERS, ZMEDIAITEM_ROOTFOLDER, &hRootList);
	hr = ZMediaLib_CreateList(ZMEDIALIST_TYPE_ALL_VIDEOS, ZMEDIAITEM_ROOTFOLDER, &hRootList);

	size_t cItemsRoot = 0;
	hr = ZMediaList_GetItemCount(hRootList, &cItemsRoot);

	return (uint16)cItemsRoot;
}

//void WindowsPhoneRuntimeComponent::myfunc(Platform::WriteOnlyArray<uint8>^ intOutArray, Platform::String^* strVideoFilename) {
//	int i = 0;
//
//	auto_ZMediaLibRequirement myLib;
//	myLib.Require();
//	HZMEDIALIST* phList = NULL;
//	ULONGLONG listSize = 0;
//	ULONG pullCount = 0;
//
//	HRESULT hr = 0;
//
//	HZMEDIALIST hRootList = NULL;
//	//hr = ZMediaLib_CreateList(ZMEDIALIST_TYPE_FOLDER_FOLDERS, ZMEDIAITEM_ROOTFOLDER, &hRootList);
//	hr = ZMediaLib_CreateList(ZMEDIALIST_TYPE_ALL_VIDEOS, ZMEDIAITEM_ROOTFOLDER, &hRootList);
//
//	size_t cItemsRoot = 0;
//	hr = ZMediaList_GetItemCount(hRootList, &cItemsRoot);
//
//	////setting array size to number of videos
//	//IVector<IVector<uint8>^>^ doubleArray;
//
//	ZMEDIAITEM *rgItemsRoot = (ZMEDIAITEM*)malloc(sizeof(ZMEDIAITEM) * cItemsRoot);
//
//
//	hr = ZMediaList_GetItems(hRootList, 0, rgItemsRoot, cItemsRoot, &cItemsRoot);
//
//	// Find the folder with a name matching c_wszRootPictureFolder
//	vector<vector<uint8>> doubleArray(cItemsRoot);
//
//	LPWSTR wszName = NULL;
//	ZMEDIAITEM zmiPicturesFolder = NULL;
//
//	size_t cch = 0;
//
//	for(int i=0;i<cItemsRoot;i++)
//	{
//
//		//Get the thumbnail
//		hr = ZMediaLib_GetItemThumbnail(rgItemsRoot[0], ZMEDIAITEM_THUMBTYPE_NORMAL, NULL, 0, &cch);
//		byte* myThumbData = (byte*) malloc(cch);// *sizeof(WCHAR));
//		hr = ZMediaLib_GetItemThumbnail(rgItemsRoot[0], ZMEDIAITEM_THUMBTYPE_NORMAL, (void*)myThumbData, cch, NULL);
//
//		//Now get the original asset
//		ZMEDIAITEM *pItem = (ZMEDIAITEM*)malloc(sizeof(ZMEDIAITEM));
//		hr = ZMediaList_GetItem(hRootList, 0, pItem);
//
//		ZMEDIAITEMSTREAM *pStream = (ZMEDIAITEMSTREAM*)malloc(sizeof(ZMEDIAITEMSTREAM));
//		hr = ZMediaLib_GetItemStreamOnProperty(*pItem, ZMEDIAITEM_ATTRIBUTE_FILEPATH, pStream);
//
//		ULARGE_INTEGER *pSize = (ULARGE_INTEGER *)malloc(sizeof(ULARGE_INTEGER));
//		hr = ZMediaLib_GetSizeItemStream(*pStream, pSize);
//
//		vector<byte> element(cch);
//		//doubleArray.push_back(element);
//		for (int iter = 0; iter < cch; iter++)
//		{
//			element[iter]=( (uint8) myThumbData[iter]);
//		}
//		doubleArray[i]=element;
//	}
//	// get filename
//	/*hr = ZMediaLib_GetItemStringAttribute(rgItemsRoot[0], ZMEDIAITEM_ATTRIBUTE_FILENAME, NULL, NULL, &cch);
//	WCHAR *str = new WCHAR[cch];
//	hr = ZMediaLib_GetItemStringAttribute(rgItemsRoot[0], ZMEDIAITEM_ATTRIBUTE_FILENAME, str, cch, &cch);
//
//	delete str;
//	hr = ZMediaLib_GetItemStringAttribute(rgItemsRoot[0], ZMEDIAITEM_ATTRIBUTE_FILEPATH, NULL, NULL, &cch);
//	str = new WCHAR[cch]; 
//	hr = ZMediaLib_GetItemStringAttribute(rgItemsRoot[0], ZMEDIAITEM_ATTRIBUTE_FILEPATH, str, cch, &cch);
//	*strVideoFilename = ref new String(str);*/
//
//	//CREATEFILE2_EXTENDED_PARAMETERS extendedParams = { 0 };
//	//extendedParams.dwSize = sizeof(CREATEFILE2_EXTENDED_PARAMETERS);
//	//extendedParams.dwFileAttributes = FILE_ATTRIBUTE_NORMAL;
//	//extendedParams.dwFileFlags = FILE_FLAG_SEQUENTIAL_SCAN;
//	//extendedParams.dwSecurityQosFlags = SECURITY_ANONYMOUS;
//	//extendedParams.lpSecurityAttributes = nullptr;
//	//extendedParams.hTemplateFile = nullptr;
//
//	//HANDLE hFile = CreateFile2(str, GENERIC_READ, FILE_SHARE_READ, OPEN_EXISTING, &extendedParams);
//
//	//delete str;
//	//str = new WCHAR[extendedParams.dwSize];
//	//DWORD dNumberOfBytesRead = 0;
//	//BOOL readSuccess = ReadFile(hFile, (void *) str, extendedParams.dwSize, &dNumberOfBytesRead, NULL);
//}

