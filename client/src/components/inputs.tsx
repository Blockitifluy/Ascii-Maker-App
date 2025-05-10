import { Component, createContext, useContext } from "solid-js";
import { ComponentChildrenProps, StoreSignal } from "../lib/helper";
import { createStore } from "solid-js/store";

export interface AsciiParams {
	Image: File | null;
	Size: number;
}

const InitAsciiParams: AsciiParams = {
	Size: 50,
	Image: null
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
	const updateFormField = (fieldName: keyof AsciiParams) => (event: Event) => {
		const inputElement = event.currentTarget as HTMLInputElement;

		const value = GetValueFromType(
			inputElement
		) as AsciiParams[typeof fieldName];

		SetAsciiParams(fieldName, value);
		console.log(GetAsciiParams());
	};

	return (
		<div id='inputs'>
			<label for='image-input'>Upload an image to convert</label>
			<input
				type='file'
				id='image-input'
				accept='image/png, image/jpeg'
				on:change={updateFormField("Image")}
			/>
			<input
				type='number'
				id='size'
				on:change={updateFormField("Size")}
				value={GetAsciiParams().Size}
			/>
			<button class='convert'>Convert</button>
		</div>
	);
};

export default Inputs;
