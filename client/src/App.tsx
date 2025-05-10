import { Component } from "solid-js";
import Header from "./components/header";
import Inputs, { InputContext } from "./components/inputs";
import "./app.scss";

const App: Component = () => {
	return (
		<InputContext>
			<Header />

			<div id='content'>
				<p>
					An easy way to convert any image into ascii art. Quick, Easy, No
					payment ever!
				</p>

				<img id='source-image' />

				<Inputs />

				<sub>By Blockitifluy</sub>
			</div>
		</InputContext>
	);
};

export default App;
