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
