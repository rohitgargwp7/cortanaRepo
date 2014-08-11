﻿// WindowsPhoneRuntimeComponent1.cpp
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
	rgItemsRoot =NULL;
}

Platform::Array<uint8>^ WindowsPhoneRuntimeComponent::GetVideoInfo(uint8 position,  Platform::String^* strVideoFilePath,  Platform::String^* strVideoFilename, Platform::String^* albumName, double* videoTime,int* videoDuration,int *videoSize) {

	auto_ZMediaLibRequirement myLib;

	myLib.Require();

	HRESULT hr = 0;
	size_t cch = 0;

	//Get the thumbnail
	hr = ZMediaLib_GetItemThumbnail(rgItemsRoot[position], ZMEDIAITEM_THUMBTYPE_NORMAL, NULL, 0, &cch);//thumnail could be fetcehed tiny too
	byte* myThumbData = (byte*) malloc(cch);// *sizeof(WCHAR));
	hr = ZMediaLib_GetItemThumbnail(rgItemsRoot[position], ZMEDIAITEM_THUMBTYPE_NORMAL, (void*)myThumbData, cch, NULL);

	Platform::Array<uint8>^ intOutArray=ref new Platform::Array<uint8>(cch);

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
	delete str;


	/*ZMEDIAITEM * zMedia = (ZMEDIAITEM*)malloc(sizeof(ZMEDIAITEM));
	hr = ZMediaLib_GetRelatedItem(rgItemsRoot[position], ZMEDIAITEM_RELATION_FOLDER,zMedia);*/
	//str = new WCHAR[cch]; 
	//hr = ZMediaLib_GetItemStringAttribute(rgItemsRoot[position], ZMEDIAITEM_ATTRIBUTE_SOURCE, str, cch, &cch);
	//*albumName = ref new String(str);
	//delete str;
	
	hr = ZMediaLib_GetItemIntAttribute(rgItemsRoot[position], ZMEDIAITEM_ATTRIBUTE_DURATION, videoDuration);

	hr = ZMediaLib_GetItemIntAttribute(rgItemsRoot[position], ZMEDIAITEM_ATTRIBUTE_FILESIZE, videoSize);
	/*hr = ZMediaLib_GetItemStringAttribute(rgItemsRoot[position], ZMEDIAITEM_ATTRIBUTE_ALBUM, NULL, NULL, &cch);
	str = new WCHAR[cch]; 
	hr = ZMediaLib_GetItemStringAttribute(rgItemsRoot[position], ZMEDIAITEM_ATTRIBUTE_ALBUM, str, cch, &cch);
	*strVideoAlbum = ref new String(str);
	delete str;*/

	FILETIME ft;
	hr=ZMediaLib_GetItemDateTimeAttribute(rgItemsRoot[position],ZMEDIAITEM_ATTRIBUTE_DATE,&ft);

	*videoTime= (((ULONGLONG) ft.dwHighDateTime) << 32) + ft.dwLowDateTime;

	return intOutArray;
}

uint16 WindowsPhoneRuntimeComponent::GetVideoCount()
{
	auto_ZMediaLibRequirement myLib;

	myLib.Require();

	HRESULT hr = 0;
	HZMEDIALIST hRootList = NULL;
	//hr = ZMediaLib_CreateList(ZMEDIALIST_TYPE_FOLDER_FOLDERS, ZMEDIAITEM_ROOTFOLDER, &hRootList);
	hr = ZMediaLib_CreateList(ZMEDIALIST_TYPE_ALL_VIDEOS, ZMEDIAITEM_ROOTFOLDER, &hRootList);

	size_t cItemsRoot = 0;
	hr = ZMediaList_GetItemCount(hRootList, &cItemsRoot);

	rgItemsRoot = (ZMEDIAITEM*)malloc(sizeof(ZMEDIAITEM) * cItemsRoot);

	hr = ZMediaList_GetItems(hRootList, 0, rgItemsRoot, cItemsRoot, &cItemsRoot);

	return (uint16)cItemsRoot;
}

void WindowsPhoneRuntimeComponent::ClearData()
{
	rgItemsRoot =NULL;
}

