#pragma once

#include <stdint.h>

class NvEncoderD3D11;

namespace nvenc
{

	class NvEncoder
	{
		public:
			NvEncoder(int width, int height);
			~NvEncoder();

			int EncodeFrame(ID3D11Texture2D* texture, uint8_t* outputBuffer, int outputBufferSize);

		private:

			int nPackets = 0;
			int width, height;

			NvEncoderD3D11* encoder = nullptr;
	};
}