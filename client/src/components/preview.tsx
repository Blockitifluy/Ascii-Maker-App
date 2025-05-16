import { Component, createResource, createSignal, Show } from "solid-js";
import { GetAsciiParams } from "./inputs";
import { ConvertToAscii } from "../lib/api";
import { SetFeedback } from "./feedback-message";

const Preview: Component = () => {
	const [getAscii, _] = createSignal<string>("");
	const [ascii] = createResource(getAscii, async () => {
		const ascii = GetAsciiParams();

		try {
			const rAscii = await ConvertToAscii(ascii);

			return rAscii;
		} catch (e) {
			const ex = e as Error;

			SetFeedback({ IsError: true, Message: ex.message });
			return "";
		}
	});

	return (
		<Show when={(!ascii.loading && !ascii.error) || import.meta.env.DEV}>
			<pre id='preview'>{ascii()}</pre>
		</Show>
	);
};

export default Preview;
