#pragma once
#include "zmedialib.h"
using namespace Platform;
using namespace Windows::Foundation::Collections;

namespace RPALApiComponent
{

	public ref class FetchPreRecordedVideos sealed
	{
		ZMEDIAITEM *rgItemsRoot;
		auto_ZMediaLibRequirement myLib;
	public:
		FetchPreRecordedVideos();
		Platform::Array<uint8>^ GetVideoInfo(uint8 position, Platform::String^* strVideoFilePath,Platform::String^* strVideoFilename,Platform::String^* albumName,float64* videoDate,int* videoDuration,int *videoSize);
		uint16 GetVideoCount();
		void ClearData();
	};
}