#pragma once

/**
 * Simple facade around the more complicated NvEncFacadeD3D11 classes
 */
class NvEncFacadeD3D11 final
{
public:
	NvEncFacadeD3D11(int width, int height, int bitrate, int targetFrameRate, int extraOutputDelay = 3);
	~NvEncFacadeD3D11();

	/** For best performance, set the vPacket to large capacity */
	void EncodeFrame(struct ID3D11Texture2D* source, std::vector<uint8_t>& vPacket);

	void SetBitrate(int bitrate, int targetFrameRate);

private:

	int width;
	int height;
	int bitrate;
	int targetFrameRate;
	int extraOutputDelay;

	int nPackets = 0;
	bool doReconfigure = false;
	class NvEncoderD3D11* encoder = nullptr;

	void Reconfigure() const;
};
