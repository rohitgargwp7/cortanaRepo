#include "RPALApiComponent.h"
#include "zmedialib.h"
using namespace RPALApiComponent;
using namespace Platform;
using namespace Windows::Foundation::Collections; 

FetchPreRecordedVideos::FetchPreRecordedVideos()
{
	myLib.Require();
	_rgItemsRoot =NULL;
}

void FetchPreRecordedVideos::ClearData()
{
	myLib.Release();
	delete _rgItemsRoot;
}

// Function to get video count and store the video list in _rgItemsRoot
uint16 FetchPreRecordedVideos::GetVideoCount()
{
	HRESULT hr = 0;
	HZMEDIALIST hRootList = NULL;
	hr = ZMediaLib_CreateList(ZMEDIALIST_TYPE_ALL_VIDEOS, ZMEDIAFOLDER_TYPE_ROOT, &hRootList);


	HZMEDIALIST finalList = NULL;
	const WCHAR *pictureLibrayPath = L"C:/Data/Users/Public/Pictures";
	ZMEDIAITEM_STRINGATTRIBUTE * atbs = new ZMEDIAITEM_STRINGATTRIBUTE[1];
	atbs[0] = ZMEDIAITEM_ATTRIBUTE_FILEPATH;
	hr = ZMediaList_FilterList(hRootList,atbs,1,pictureLibrayPath,&finalList);



	size_t cItemsRoot = 0;
	hr = ZMediaList_GetItemCount(hRootList, &cItemsRoot);

	_rgItemsRoot = new ZMEDIAITEM[cItemsRoot];
	hr = ZMediaList_GetItems(hRootList, 0, _rgItemsRoot, cItemsRoot, &cItemsRoot);

	return (uint16)cItemsRoot;
}

// Function to get a video file info using its position in the _rgItemsRoot
Array<byte>^ FetchPreRecordedVideos::GetVideoInfo(uint8 position,  Platform::String^* strVideoFilePath, double* videoTime,int* videoDuration,int *videoSize) 
{
	HRESULT hr = 0;
	size_t cch = 0;

	//Get the thumbnail
	hr = ZMediaLib_GetItemThumbnail(_rgItemsRoot[position], ZMEDIAITEM_THUMBTYPE_NORMAL, NULL, 0, &cch);//thumnail could be fetcehed tiny too
	byte* myThumbData = new byte[cch];// *sizeof(WCHAR));
	hr = ZMediaLib_GetItemThumbnail(_rgItemsRoot[position], ZMEDIAITEM_THUMBTYPE_NORMAL, (void*)myThumbData, cch, NULL);
	Platform::Array<byte>^ intOutArray=ref new Platform::Array<byte>(myThumbData, cch);
	delete myThumbData;

	// get filepath
	hr = ZMediaLib_GetItemStringAttribute(_rgItemsRoot[position], ZMEDIAITEM_ATTRIBUTE_FILEPATH, NULL, NULL, &cch);
	WCHAR *str = new WCHAR[cch]; 
	hr = ZMediaLib_GetItemStringAttribute(_rgItemsRoot[position], ZMEDIAITEM_ATTRIBUTE_FILEPATH, str, cch, &cch);
	*strVideoFilePath = ref new String(str);
	delete str;

	//get file duration and size
	hr = ZMediaLib_GetItemIntAttribute(_rgItemsRoot[position], ZMEDIAITEM_ATTRIBUTE_DURATION, videoDuration);
	hr = ZMediaLib_GetItemIntAttribute(_rgItemsRoot[position], ZMEDIAITEM_ATTRIBUTE_FILESIZE, videoSize);

	//get file creation date
	FILETIME ft;
	hr=ZMediaLib_GetItemDateTimeAttribute(_rgItemsRoot[position],ZMEDIAITEM_ATTRIBUTE_DATE,&ft);
	*videoTime= (((ULONGLONG) ft.dwHighDateTime) << 32) + ft.dwLowDateTime;

	return intOutArray;
}

void FetchPreRecordedVideos::GetFolderInfo(uint8 position,Platform::String^* strVideoFilePath)
{
	HRESULT hr = 0;
	size_t cch = 0;

	// get filepath
	hr = ZMediaLib_GetItemStringAttribute(_rgItemsRoot[position], ZMEDIAITEM_ATTRIBUTE_FILEPATH, NULL, NULL, &cch);
	WCHAR *str = new WCHAR[cch]; 
	hr = ZMediaLib_GetItemStringAttribute(_rgItemsRoot[position], ZMEDIAITEM_ATTRIBUTE_FILEPATH, str, cch, &cch);
	*strVideoFilePath = ref new String(str);
	delete str;
}