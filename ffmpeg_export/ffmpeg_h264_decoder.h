#ifndef __FFMPEG_H264_DECODER_H__
#define __FFMPEG_H264_DECODER_H__
#include <stdio.h>

#include "libavcodec/avcodec.h"
#include "libavformat/avformat.h"
#include "libavfilter/avfilter.h"
#include "libswresample/swresample.h"
#include "libavdevice/avdevice.h"
#include "libavutil/avutil.h"
#include "libpostproc/postprocess.h"
#include "libswscale/swscale.h"


static AVCodec * codec;
static AVCodecContext * context;
static AVFrame * frame_yuv;
static AVPacket packet;
static int codec_init = 0;
static int test_value = 0;
//For Converting betweeen colorspaces
AVFrame* avFrameYUV = NULL;
AVFrame* avFrameRGB = NULL;
struct SwsContext* ctxt = NULL;
//methods to open and close device, either video capture or file
__declspec(dllexport) int decode_packet(char * data, int length, int width, int height);
__declspec(dllexport) char * getFrame();
//test static variables
__declspec(dllexport) int getValue();
__declspec(dllexport) void initFrameConverter(int width, int height);
__declspec(dllexport) char * convertYUVtoRGB(char * yuv, int width, int height);
__declspec(dllexport) void deinitFrameConverter();
#endif