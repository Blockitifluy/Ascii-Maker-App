import { AsciiParams } from "../components/inputs";

export const BaseURL = "http://localhost:8000/";

export async function UploadImage(blob: Blob): Promise<string> {
	const req: RequestInit = {
		method: "POST",
		body: blob,
		headers: {
			"Content-Type": blob.type
		}
	};

	const res = await fetch(BaseURL + "api/image", req);
	if (!res.ok) {
		throw new Error(`Uploading Image wasn't ok: ${res.status}`);
	}

	return res.text();
}

export async function ConvertToAscii(
	asciiParams: AsciiParams
): Promise<string> {
	const url = BaseURL + `api/convert-image-to-ascii?id=${asciiParams.ImageID}`;

	const res = await fetch(url, {
		method: "POST",
		body: JSON.stringify(asciiParams)
	});
	if (!res.ok) {
		throw new Error(`Converting image wasn't ok: ${res.status}`);
	}

	return res.text();
}
