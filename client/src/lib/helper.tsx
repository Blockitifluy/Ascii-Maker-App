import { JSX } from "solid-js";
import { SetStoreFunction } from "solid-js/store";
import { AsciiParams } from "../components/inputs";

export interface ComponentChildrenProps {
	children: JSX.Element;
}

export type valueOf<T> = T[keyof T];
export type StoreSignal<T> = [T, SetStoreFunction<AsciiParams>];
