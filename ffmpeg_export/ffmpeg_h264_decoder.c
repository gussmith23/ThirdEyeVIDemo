#include "ffmpeg_h264_decoder.h"


__declspec(dllexport) int decode_packet(char * data, int length, int width, int height) {
	printf("Function Called\n");
	if (codec_init < 1) {
		codec_init = 2;
		printf("Initing Called\n");
		avcodec_register_all();
		avformat_network_init();
		codec = avcodec_find_decoder(AV_CODEC_ID_H264);
		if (!codec)
		{
			printf("Codec not found\n");

			return 0;
		}
		context = avcodec_alloc_context3(codec);
		if (!context)
		{
			printf("Could not allocate video codec context\n");
			return 0;
		}
		avcodec_get_context_defaults3(context, codec);
		context->flags |= CODEC_FLAG_LOW_DELAY;
		context->flags2 |= CODEC_FLAG2_CHUNKS;

		context->bit_rate = 4000000;
		/* resolution must be a multiple of two */
		context->width = width;
		context->height = height;
		/* frames per second */
		context->time_base = (AVRational) { 1, 30 };
		context->gop_size = 1; /* emit one intra frame every ten frames */
		context->pix_fmt = AV_PIX_FMT_YUV420P;
		//context->thread_count = 4; //become higher?
		//context->thread_type = FF_THREAD_SLICE;
		//context->strict_std_compliance = FF_COMPLIANCE_EXPERIMENTAL;
		//set pixel formal
		if (avcodec_open2(context, codec, NULL) < 0)
		{
			printf("Could not open codec\n");
			return 0;
		}
		frame_yuv = av_frame_alloc();
		if (!frame_yuv)
		{
			printf("Could not allocate video frame\n");
			return 0;
		}
	}
	printf("Finished Init\n");
	AVPacket packet;
	av_init_packet(&packet);
	packet.pts = AV_NOPTS_VALUE;
	packet.dts = AV_NOPTS_VALUE;
	packet.data = (uint8_t *)data;// frame data
	packet.size = (int)length;// frame data size
	int got_frame = 0;
	int len = avcodec_decode_video2(context, frame_yuv, &got_frame, &packet);
	if (len >= 0 && got_frame)
	{
		//do things with the data, like converting it to RGB, resizing it etc.
		printf("Key Frame: %i\n", frame_yuv->key_frame);
		enum AVColorSpace av = av_frame_get_colorspace(frame_yuv);
		printf("AVColorSpace: %i\n", av);
		struct SwsContext* tmpCxt = sws_getCachedContext(NULL,
			frame_yuv->width,
			frame_yuv->height,
			AV_PIX_FMT_YUV420P,
			frame_yuv->width,
			frame_yuv->height,
			AV_PIX_FMT_RGBA,
			SWS_BICUBIC,
			NULL,
			NULL,
			NULL
			);
		sws_scale(tmpCxt, frame_yuv->data, frame_yuv->linesize, 0, frame_yuv->height, avFrameRGB->data, avFrameRGB->linesize);
		sws_freeContext(tmpCxt);
	}else{
		printf("No Frame\n");
		len = 0;
	}
	av_register_all();
	return len;
}
__declspec(dllexport) char * getFrame() {
	return (char *)avFrameRGB->data[0];
}
__declspec(dllexport) int getValue() {
	if (test_value == 0) {
		test_value = 42;
		return test_value;
	}
	else {
		return test_value++;
	}
}
__declspec(dllexport) char * convertYUVtoRGB(char * yuv, int width, int height) {
	struct SwsContext* tmpCxt = sws_getCachedContext(NULL,
		width,
		height,
		AV_PIX_FMT_NV21,
		width,
		height,
		AV_PIX_FMT_RGBA,
		SWS_BICUBIC,
		NULL,
		NULL,
		NULL
		);

	/* fill the yuv frame with the raw data */
	avpicture_fill((AVPicture *)avFrameYUV, yuv, AV_PIX_FMT_NV21, width, height);
	/* perform the conversion */
	sws_scale(tmpCxt, avFrameYUV->data, avFrameYUV->linesize, 0, height, avFrameRGB->data, avFrameRGB->linesize);
	/* return the rgba data */
	return (char *)avFrameRGB->data[0];
}
__declspec(dllexport) void initFrameConverter(int width, int height) {
	if (avFrameYUV == NULL) {
		avFrameYUV = av_frame_alloc();
	}
	if (avFrameRGB == NULL) {
		avFrameRGB = av_frame_alloc();
		int bytes = avpicture_get_size(AV_PIX_FMT_RGBA, width, height);
		uint8_t * buffer = (uint8_t *)av_malloc(bytes*sizeof(uint8_t));
		avpicture_fill((AVPicture *)avFrameRGB, buffer, AV_PIX_FMT_RGBA, width, height);
	}
	if (ctxt == NULL) {
		ctxt = sws_getCachedContext(NULL,
			width,
			height,
			AV_PIX_FMT_YUV420P,
			width,
			height,
			AV_PIX_FMT_RGBA,
			SWS_BICUBIC,
			NULL,
			NULL,
			NULL
			);
	}
	printf("initFrameConverter Done!");
}
__declspec(dllexport) void deinitFrameConverter() {
		sws_freeContext(ctxt);
		av_frame_free(&avFrameYUV);
		av_frame_free(&avFrameRGB);
}






