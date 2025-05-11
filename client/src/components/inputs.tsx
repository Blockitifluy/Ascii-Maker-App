import { Component, createContext, Show, useContext } from "solid-js";
import { ComponentChildrenProps, StoreSignal } from "../lib/helper";
import { createStore } from "solid-js/store";
import { UploadImage } from "../lib/api";
import { BaseURL } from "../lib/api";

export interface AsciiParams {
	Size: number;
	ImageID: string;
}

const InitAsciiParams: AsciiParams = {
	Size: 50,
	ImageID: ""
};

const AsciiParamsContext = createContext<StoreSignal<AsciiParams>>(null!);

export const InputContext: Component<ComponentChildrenProps> = props => {
	const [asciiParams, setAsciiParams] =
		createStore<AsciiParams>(InitAsciiParams);

	AsciiParamsContext.defaultValue = [asciiParams, setAsciiParams];

	return (
		<AsciiParamsContext.Provider value={[asciiParams, setAsciiParams]}>
			{props.children}
		</AsciiParamsContext.Provider>
	);
};

export function GetAsciiParams(): AsciiParams {
	const asciiParams = useContext(AsciiParamsContext);

	if (!asciiParams) throw new Error("Ascii Parameters haven't been set!");

	return asciiParams[0];
}

export function SetAsciiParams(
	key: keyof AsciiParams,
	value: AsciiParams[typeof key]
) {
	const asciiParams = useContext(AsciiParamsContext);

	if (!asciiParams) throw new Error("Ascii Parameters haven't been set!");

	asciiParams[1](key, value);
}

function GetValueFromType(target: HTMLInputElement): unknown {
	switch (target.type) {
		case "number":
			return parseInt(target.value);
		case "file":
			return target.files?.item(0);
		default:
			return target.value;
	}
}

const Inputs: Component = () => {
	const asciiParams = GetAsciiParams();

	const updateFormField = (fieldName: keyof AsciiParams) => (event: Event) => {
		const inputElement = event.currentTarget as HTMLInputElement;

		const value = GetValueFromType(
			inputElement
		) as AsciiParams[typeof fieldName];

		SetAsciiParams(fieldName, value);
		console.log(asciiParams);
	};

	const setImage = async (event: Event) => {
		const target = event.currentTarget as HTMLInputElement,
			file = target.files?.item(0);

		if (file == null) {
			console.warn("There was no file to upload!");
			return;
		}

		const id = await UploadImage(file);

		SetAsciiParams("ImageID", id);
	};

	return (
		<div id='inputs'>
			<Show when={asciiParams.ImageID !== ""}>
				<img
					id='source-image'
					src={BaseURL + `api/image?id=${asciiParams.ImageID}`}
				/>
			</Show>

			<label for='image-input'>Upload an image to convert</label>
			<input
				type='file'
				id='image-input'
				accept='image/png, image/jpeg'
				on:change={setImage}
			/>
			<input
				type='number'
				id='size'
				on:change={updateFormField("Size")}
				value={asciiParams.Size}
			/>
			<button class='convert'>Convert</button>
		</div>
	);
};

export default Inputs;
