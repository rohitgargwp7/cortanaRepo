#pragma once
#include "zmedialib.h"
using namespace Platform;
using namespace Windows::Foundation::Collections;

namespace RPALApiComponent
{

	public ref class FetchPreRecordedVideos sealed
	{
		ZMEDIAITEM *_rgItemsRoot;
		auto_ZMediaLibRequirement myLib;
	public:
		FetchPreRecordedVideos();
		Array<byte>^ GetVideoInfo(uint8 position, float64* videoDate,int* videoDuration,int *videoSize);
		uint16 GetVideoCount();
		void ClearData();
		void GetVideoFilePath(uint8 position, Platform::String^* strVideoFilePath);
	};
}