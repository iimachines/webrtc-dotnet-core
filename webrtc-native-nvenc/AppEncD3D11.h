#pragma once

#include <stdint.h>

class NvEncoderD3D11;

namespace nvenc
{

	class NvEncoder
	{
		public:
			NvEncoder(int width, int height, uint64_t bitrate, uint32_t targetFrameRate);
			~NvEncoder();

			int EncodeFrame(ID3D11Texture2D* texture, uint8_t* outputBuffer, int outputBufferSize);

			void SetBitrate(uint64_t bitrate, uint32_t targetFrameRate);

		private:

			int nPackets = 0;
			int width, height;

			uint64_t bitrate;
			uint32_t targetFrameRate;

			bool doReconfigure = false;

			NvEncoderD3D11* encoder = nullptr;

			void Reconfigure();
	};
}