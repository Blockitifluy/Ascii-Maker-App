import { Component, createContext, Show, useContext } from "solid-js";
import {
	ComponentChildrenProps,
	GetValueFromType,
	StoreSignal
} from "../lib/helper";
import { createStore } from "solid-js/store";
import { ConvertToAscii, UploadImage } from "../lib/api";
import { BaseURL } from "../lib/api";
import Preview from "./preview";
import FeedbackComponent, { SetFeedback } from "./feedback-message";

export interface AsciiParams {
	Size: number;
	Brightness: number;
	Invert: boolean;
	ImageID: string;
}

const InitAsciiParams: AsciiParams = {
	Size: 100,
	Brightness: 1.5,
	Invert: false,
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

const Inputs: Component = () => {
	const asciiParams = () => GetAsciiParams();

	const updateFormField = (fieldName: keyof AsciiParams) => (event: Event) => {
		const inputElement = event.currentTarget as HTMLInputElement;

		const value = GetValueFromType(
			inputElement
		) as AsciiParams[typeof fieldName];

		SetAsciiParams(fieldName, value);
	};

	const setImage = async (event: Event) => {
		const target = event.currentTarget as HTMLInputElement,
			file = target.files?.item(0);

		if (file == null) {
			console.warn("There was no file to upload!");
			return;
		}

		try {
			const id = await UploadImage(file);

			SetAsciiParams("ImageID", id);
		} catch (e) {
			const ex = e as Error;

			SetFeedback({ IsError: true, Message: ex.message });
		}
	};

	const copyResult = async () => {
		try {
			const ascii = await ConvertToAscii(asciiParams());

			navigator.clipboard.writeText(ascii);
		} catch (e) {
			const ex: Error = e as Error;

			SetFeedback({
				IsError: true,
				Message: ex.message
			});
		}
	};

	return (
		<div id='inputs'>
			<Show when={asciiParams().ImageID || import.meta.env.DEV}>
				<div id='preview-menu'>
					<img
						id='source-image'
						src={BaseURL + `api/image?id=${asciiParams().ImageID}`}
					/>
					<Preview />
				</div>
			</Show>

			<label for='image-input' class='button-like'>
				Upload an image to convert
			</label>
			<input
				type='file'
				id='image-input'
				accept='image/png, image/jpeg'
				on:change={setImage}
			/>

			<FeedbackComponent />

			<label for='size-input' class='underlined'>
				Size
			</label>
			<input
				type='number'
				id='size-input'
				on:change={updateFormField("Size")}
				value={asciiParams().Size}
			/>

			<label for='bright-input' class='underlined'>
				Brightness
			</label>
			<input
				type='number'
				id='bright-input'
				on:change={updateFormField("Brightness")}
				value={asciiParams().Brightness}
			/>

			<div>
				<label for='invert-input' class='underlined'>
					Invert:
				</label>
				<input
					type='checkbox'
					id='invert-input'
					on:change={updateFormField("Invert")}
					checked={asciiParams().Invert}
				/>
			</div>

			<button class='convert' on:mousedown={copyResult}>
				Convert
			</button>
		</div>
	);
};

export default Inputs;
