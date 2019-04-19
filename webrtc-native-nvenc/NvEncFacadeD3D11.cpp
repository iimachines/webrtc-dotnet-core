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

#include "pch.h"
#include "NvCodec/NvEncoder/NvEncoderD3D11.h"
#include "NvCodec/NvEncoder/NvEncoder.h"
#include "NvEncFacadeD3D11.h"

using Microsoft::WRL::ComPtr;

	NvEncFacadeD3D11::NvEncFacadeD3D11(int width, int height, int bitrate, int targetFrameRate)
		: width(width)
		, height(height)
		, bitrate(bitrate)
		, targetFrameRate(targetFrameRate)
	{
	}

	void NvEncFacadeD3D11::SetBitrate(int bitrate, int targetFrameRate)
	{
		this->bitrate = bitrate;
		this->targetFrameRate = targetFrameRate;
		this->doReconfigure = true;
	}

	void NvEncFacadeD3D11::Reconfigure() const
	{
		// printf("UPDATE target frame rate to %d and bitrate to %d\n", this->targetFrameRate, this->bitrate);

		NV_ENC_CONFIG config;
		memset(&config, 0, sizeof(config));
		config.version = NV_ENC_CONFIG_VER;
		config.rcParams.averageBitRate = bitrate;

		NV_ENC_RECONFIGURE_PARAMS reconfigureParams;
		memset(&reconfigureParams, 0, sizeof(reconfigureParams));
		reconfigureParams.version = NV_ENC_RECONFIGURE_PARAMS_VER;
		reconfigureParams.resetEncoder = 1;
		reconfigureParams.forceIDR = 1;
		reconfigureParams.reInitEncodeParams.encodeConfig = &config;

		encoder->GetInitializeParams(&reconfigureParams.reInitEncodeParams);
		reconfigureParams.reInitEncodeParams.frameRateNum = targetFrameRate;
		reconfigureParams.reInitEncodeParams.frameRateDen = 1;

		encoder->Reconfigure(&reconfigureParams);
	}

	void NvEncFacadeD3D11::EncodeFrame(ID3D11Texture2D* source, std::vector<uint8_t>& vPacket)
	{
		// get the device & context of the source texture
		ID3D11Device* device;
		ID3D11DeviceContext* pContext;
		source->GetDevice(&device);
		device->GetImmediateContext(&pContext);

		// if the encoder isn't created yet, we do so now
		if (encoder == nullptr)
		{
			encoder = new NvEncoderD3D11(device, width, height, NV_ENC_BUFFER_FORMAT_ARGB);

			// create the initial structures to hold the config
			NV_ENC_INITIALIZE_PARAMS initializeParams = { NV_ENC_INITIALIZE_PARAMS_VER };
			NV_ENC_CONFIG encodeConfig = { NV_ENC_CONFIG_VER };
			initializeParams.encodeConfig = &encodeConfig;

			// fill them with default values
			encoder->CreateDefaultEncoderParams(&initializeParams, NV_ENC_CODEC_H264_GUID, NV_ENC_PRESET_LOW_LATENCY_HQ_GUID);

			// override some values to configure them according to the requirements of the user
			/*initializeParams.frameRateNum = this->targetFrameRate;
			initializeParams.frameRateDen = 1;*/
			//printf("INIT target frame rate to %d and bitrate to %d\n", this->targetFrameRate, this->bitrate);

			// set the max bit rate
			encodeConfig.rcParams.averageBitRate = bitrate;
			encodeConfig.rcParams.rateControlMode = NV_ENC_PARAMS_RC_CBR_LOWDELAY_HQ;

			// these are the recommended settings for low-latency use cases like game streaming,
			// as defined in the 9.0 documentation by nVidia.
			encodeConfig.rcParams.disableBadapt = 1;
			encodeConfig.rcParams.vbvBufferSize = encodeConfig.rcParams.averageBitRate * initializeParams.frameRateDen / initializeParams.frameRateNum; // bitrate / framerate = one frame
			encodeConfig.gopLength = NVENC_INFINITE_GOPLENGTH;
			encodeConfig.rcParams.enableAQ = 1;

			encoder->CreateEncoder(&initializeParams);

			// if we triggered a reconfigure before this point, we don't need to do it anymore,
			// since it is already dealt with by the encoder creation.
			doReconfigure = false;
		}

		// Reconfigure the encoder if requested.
		if (doReconfigure)
		{
			doReconfigure = false;
			Reconfigure();
		}

		// copy the frame into an internal buffer of nvEnc so we can encode it
		const NvEncInputFrame* encoderInputFrame = encoder->GetNextInputFrame();
		const auto target = reinterpret_cast<ID3D11Texture2D*>(encoderInputFrame->inputPtr);
		pContext->CopyResource(target, source);
		encoder->EncodeFrame(vPacket);
	}

	NvEncFacadeD3D11::~NvEncFacadeD3D11()
	{
		if (encoder)
		{
			// Flush! This means that some packets might be lost and never sent, because we don't do anything with it here.
			// [PV] Uncommented for now, since this is also done by the NvEncoder itself (at least under some conditions)
			// std::vector<uint8_t> vPacket;
			// encoder->EndEncode(vPacket);
			encoder->DestroyEncoder();
			delete encoder;
			encoder = nullptr;
		}
	}
