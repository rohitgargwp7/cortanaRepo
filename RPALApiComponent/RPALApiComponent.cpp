#include "pch.h"
#include "RPALApiComponent.h"
#include "zmedialib.h"
using namespace RPALApiComponent;
using namespace Platform;
using namespace Windows::Foundation::Collections; 

FetchPreRecordedVideos::FetchPreRecordedVideos()
{
	myLib.Require();
	rgItemsRoot =NULL;
}

void FetchPreRecordedVideos::ClearData()
{
	myLib.Release();
	delete rgItemsRoot;
}


// Function to get video count and store the video list in rgItemsRoot
uint16 FetchPreRecordedVideos::GetVideoCount()
{
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


// Function to get a video file info using its position in the rgItemsRoot
Platform::Array<uint8>^ FetchPreRecordedVideos::GetVideoInfo(uint8 position,  Platform::String^* strVideoFilePath,  Platform::String^* strVideoFilename, Platform::String^* albumName, double* videoTime,int* videoDuration,int *videoSize) 
{
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

	
	hr = ZMediaLib_GetItemIntAttribute(rgItemsRoot[position], ZMEDIAITEM_ATTRIBUTE_DURATION, videoDuration);

	hr = ZMediaLib_GetItemIntAttribute(rgItemsRoot[position], ZMEDIAITEM_ATTRIBUTE_FILESIZE, videoSize);

	FILETIME ft;
	hr=ZMediaLib_GetItemDateTimeAttribute(rgItemsRoot[position],ZMEDIAITEM_ATTRIBUTE_DATE,&ft);

	*videoTime= (((ULONGLONG) ft.dwHighDateTime) << 32) + ft.dwLowDateTime;

	return intOutArray;
}


