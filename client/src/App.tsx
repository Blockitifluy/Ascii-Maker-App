import { Component, ErrorBoundary } from "solid-js";
import Header from "./components/header";
import Inputs, { InputContext } from "./components/inputs";
import "./app.scss";
import { FeedbackContext } from "./components/feedback-message";

const App: Component = () => {
	return (
		<ErrorBoundary
			fallback={(error, reset) => (
				<div id='major-err'>
					<h1>Major Error</h1>
					<p>{error.message}</p>
					<button on:click={reset}>Try Again</button>
				</div>
			)}
		>
			<FeedbackContext>
				<InputContext>
					<Header />
					<div id='content'>
						<p>
							An easy way to convert any image into ascii art. Quick, Easy, No
							payment ever!
						</p>

						<Inputs />
					</div>
				</InputContext>
			</FeedbackContext>
		</ErrorBoundary>
	);
};

export default App;
