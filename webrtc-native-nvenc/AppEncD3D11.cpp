/*
* Copyright 2017-2019 NVIDIA Corporation.  All rights reserved.
*
* Please refer to the NVIDIA end user license agreement (EULA) associated
* with this source code for terms and conditions that govern your use of
* this software. Any use, reproduction, disclosure, or distribution of
* this software and related documentation outside the terms of the EULA
* is strictly prohibited.
*
*/

#include <d3d11.h>
#include <iostream>
#include <unordered_map>
#include <memory>
#include <wrl.h>
#include "NvCodec/NvEncoder/NvEncoderD3D11.h"
#include "NvCodec/NvEncoder/NvEncoder.h"

#include "AppEncD3D11.h"

using Microsoft::WRL::ComPtr;

namespace nvenc
{

	NvEncoder::NvEncoder(int width, int height)
	{
		this->width = width;
		this->height = height;
	}


	int NvEncoder::EncodeFrame(ID3D11Texture2D* texture, uint8_t* outputBuffer, int outputBufferSize)
	{
		std::vector<std::vector<uint8_t>> vPacket;

		// get the device & context of the source texture
		ID3D11Texture2D *source = reinterpret_cast<ID3D11Texture2D*>(texture);
		ID3D11Device* device;
		ID3D11DeviceContext* pContext;
		source->GetDevice(&device);
		device->GetImmediateContext(&pContext);

		// if the encoder isn't created yet, we do so now
		if (encoder == nullptr)
		{
			encoder = new NvEncoderD3D11(device, width, height, NV_ENC_BUFFER_FORMAT_ARGB);
			NV_ENC_INITIALIZE_PARAMS initializeParams = { NV_ENC_INITIALIZE_PARAMS_VER };
			NV_ENC_CONFIG encodeConfig = { NV_ENC_CONFIG_VER };
			initializeParams.encodeConfig = &encodeConfig;
			encoder->CreateDefaultEncoderParams(&initializeParams, NV_ENC_CODEC_H264_GUID, NV_ENC_PRESET_DEFAULT_GUID);
			encoder->CreateEncoder(&initializeParams);

		}

		// copy the frame into an internal buffer of nvEnc so we can encode it
		const NvEncInputFrame* encoderInputFrame = encoder->GetNextInputFrame();
		ID3D11Texture2D *target = reinterpret_cast<ID3D11Texture2D*>(encoderInputFrame->inputPtr);
		pContext->CopyResource(target, source);
		encoder->EncodeFrame(vPacket);

		// process the packets and put them into one big buffer
		nPackets += (int)vPacket.size();
		int nBytes = 0;
		for (std::vector<uint8_t> &packet : vPacket)
		{
			nBytes += packet.size();
		}
		int offset = 0;
		for (int i = 0; i < vPacket.size(); ++i)
		{
			std::vector<uint8_t>& packet = vPacket[i];

			uint8_t* data = packet.data();
			int size = packet.size();

			memcpy(outputBuffer + offset, data, size);
			offset += size;
		}

		return nBytes;
	}

	NvEncoder::~NvEncoder()
	{
		if (encoder == nullptr) return;

		// flush! This means that some packets might be lost and never sent,
		// because we don't do anything with it here.
		std::vector<std::vector<uint8_t>> vPacket;
		encoder->EndEncode(vPacket);

		encoder->DestroyEncoder();
		encoder = nullptr;
	}
}
