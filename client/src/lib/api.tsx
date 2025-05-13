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

	const ID = await res.text();

	return ID;
}

export function GetAsciiDownloadURL(asciiParams: AsciiParams): string {
	return (
		BaseURL +
		`api/convert-image-to-ascii?id=${asciiParams.ImageID}&size=${asciiParams.Size}&bright=${asciiParams.Brightness}`
	);
}

export async function ConvertToAscii(
	asciiParams: AsciiParams
): Promise<string> {
	const url = GetAsciiDownloadURL(asciiParams);

	const res = await fetch(url);
	if (!res.ok) {
		throw new Error(`Converting image wasn't ok: ${res.status}`);
	}

	const ascii = await res.text();

	return ascii;
}
