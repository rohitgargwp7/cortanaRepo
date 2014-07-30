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

Platform::Array<uint8>^ WindowsPhoneRuntimeComponent::myfunc(uint8 position,  Platform::String^* strVideoFilePath,Platform::String^* strVideoFilename,Platform::String^* strVideoAlbumName) {
	int i = 0;
	auto_ZMediaLibRequirement myLib;

	myLib.Require();
	
	HRESULT hr = 0;

	HZMEDIALIST hRootList = NULL;
	//hr = ZMediaLib_CreateList(ZMEDIALIST_TYPE_FOLDER_FOLDERS, ZMEDIAITEM_ROOTFOLDER, &hRootList);
	hr = ZMediaLib_CreateList(ZMEDIALIST_TYPE_ALL_VIDEOS, ZMEDIAITEM_ROOTFOLDER, &hRootList);

	size_t cItemsRoot = 0;
	hr = ZMediaList_GetItemCount(hRootList, &cItemsRoot);

	ZMEDIAITEM *rgItemsRoot = (ZMEDIAITEM*)malloc(sizeof(ZMEDIAITEM) * cItemsRoot);


	hr = ZMediaList_GetItems(hRootList, 0, rgItemsRoot, cItemsRoot, &cItemsRoot);

	// Find the folder with a name matching c_wszRootPictureFolder


	size_t cch = 0;

	//Get the thumbnail
	hr = ZMediaLib_GetItemThumbnail(rgItemsRoot[position], ZMEDIAITEM_THUMBTYPE_NORMAL, NULL, 0, &cch);//thumnail could be fetcehed tiny too
	byte* myThumbData = (byte*) malloc(cch);// *sizeof(WCHAR));
	hr = ZMediaLib_GetItemThumbnail(rgItemsRoot[position], ZMEDIAITEM_THUMBTYPE_NORMAL, (void*)myThumbData, cch, NULL);

	//Now get the original asset
	ZMEDIAITEM *pItem = (ZMEDIAITEM*)malloc(sizeof(ZMEDIAITEM));
	hr = ZMediaList_GetItem(hRootList, 0, pItem);

	ZMEDIAITEMSTREAM *pStream = (ZMEDIAITEMSTREAM*)malloc(sizeof(ZMEDIAITEMSTREAM));
	hr = ZMediaLib_GetItemStreamOnProperty(*pItem, ZMEDIAITEM_ATTRIBUTE_FILEPATH, pStream);

	ULARGE_INTEGER *pSize = (ULARGE_INTEGER *)malloc(sizeof(ULARGE_INTEGER));
	hr = ZMediaLib_GetSizeItemStream(*pStream, pSize);



	Platform::Array<uint8>^ intOutArray=ref new Platform::Array<uint8>(cch);;

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


	return intOutArray;
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

