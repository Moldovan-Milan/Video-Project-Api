import "./App.css";
import VideoPlayer from "./components/VideoPlayer";

function App() {
  return (
    <>
      <VideoPlayer src={"http://localhost:5047/api/video/6"} />
    </>
  );
}

export default App;
