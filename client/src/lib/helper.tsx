import { JSX } from "solid-js";
import { SetStoreFunction } from "solid-js/store";
import { AsciiParams } from "../components/inputs";

export interface ComponentChildrenProps {
	children: JSX.Element;
}

export type valueOf<T> = T[keyof T];
export type StoreSignal<T> = [T, SetStoreFunction<AsciiParams>];

export function GetValueFromType(target: HTMLInputElement): unknown {
	switch (target.type) {
		case "number":
			return parseFloat(target.value);
		case "file":
			return target.files?.item(0);
		case "checkbox":
			return target.checked;
		default:
			return target.value;
	}
}
