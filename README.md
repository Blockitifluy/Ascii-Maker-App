# Ascii Maker App

This is the contination of the [PNG-to-Plain-Ascii](https://github.com/Blockitifluy/PNG-to-Plain-Ascii) repository written back in 2022. The major difference is that this repo is a website with a server and latter is writen with python.

## How to Build

Run the build.bat on the root directory, to compile both the server and the client.

Compiling client:

```bat
./build-client
```

Compiling server:

```bat
./build-server
```

You can see changes on [dist](dist/) and [build folders](build/) for the client and api respectively.

## Client

The client is made with:

- Vite,
- SolidJS,
- TypeScript

## Server

The server and api is made with C# and SixLabors' [ImageSharp](https://github.com/SixLabors/ImageSharp).

## Running the App in Developement Mode

```bat
npm run dev
```

Opens [http://localhost:5173](http://localhost:5173) to view it in the browser.

## Example

| Orginal Image           | Ascii Text File                   |
| ----------------------- | --------------------------------- |
| ![cat](example/cat.jpg) | ![cat result](example/result.png) |

## Deployment

Learn more about deploying your application with the [documentations](https://vite.dev/guide/static-deploy.html).
