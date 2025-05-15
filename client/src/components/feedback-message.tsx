import {
	Component,
	createContext,
	createSignal,
	Show,
	Signal,
	useContext
} from "solid-js";
import { ComponentChildrenProps } from "../lib/helper";

export interface Feedback {
	IsError: boolean;
	Message: string;
}

export type NFeedback = Feedback | null;

const FeedContext = createContext<Signal<NFeedback>>(null!);

export const FeedbackContext: Component<ComponentChildrenProps> = props => {
	const [feedback, setFeedback] = createSignal<NFeedback>(null);

	FeedContext.defaultValue = [feedback, setFeedback];

	return (
		<FeedContext.Provider value={[feedback, setFeedback]}>
			{props.children}
		</FeedContext.Provider>
	);
};

export function GetFeedback(): NFeedback {
	const feedback = useContext(FeedContext);

	if (!feedback) return null;

	return feedback[0]();
}

export function SetFeedback(feed: NFeedback) {
	const feedback = useContext(FeedContext);

	if (!feedback) throw new Error("Feedback signal hasn't been set!");

	feedback[1](feed);
}

const FeedbackComponent: Component = () => {
	const feedback = () => GetFeedback();
	const errorClass = () => (feedback()?.IsError ? "error" : "success");

	return (
		<Show when={feedback()}>
			<div id='feedback-msg' class={errorClass()}>
				<h1>{feedback()?.IsError ? "Error" : "Message"}</h1>
				<p>{feedback()?.Message}</p>
			</div>
		</Show>
	);
};

export default FeedbackComponent;
