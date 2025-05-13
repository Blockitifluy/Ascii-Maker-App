import { Component, createResource, Show } from "solid-js";
import { GetAsciiParams } from "./inputs";
import { ConvertToAscii } from "../lib/api";

const Preview: Component = () => {
	const asciiParams = () => GetAsciiParams(),
		[ascii] = createResource(() => ConvertToAscii(asciiParams()));

	return (
		<Show when={!ascii.loading && !ascii.error}>
			<pre id='preview'>{ascii()}</pre>
		</Show>
	);
};

export default Preview;
