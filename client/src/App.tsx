import "./App.scss";

function App() {
	return (
		<>
			<h1 id='title'>Ascii Maker</h1>
			<div id='content'>
				<p>
					An easy way to convert any image into ascii art. Quick, Easy, No
					payment ever!
				</p>
				<img id='source-image' />

				<div id='inputs'>
					<input type='file' />
					<button class='convert'>Convert</button>
				</div>

				<sub>By Blockitifluy</sub>
			</div>
		</>
	);
}

export default App;
