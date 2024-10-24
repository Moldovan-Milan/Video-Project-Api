import "./App.css";
import "../node_modules/bootstrap/dist/css/bootstrap.min.css";
import VideosPage from "./pages/VideosPage";
import SingleVideo from "./pages/SingleVideo";
import UploadVideo from "./pages/UploadVideo";
import { BrowserRouter, Routes, Route } from "react-router-dom";

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route index path="/" Component={VideosPage} />
        <Route path="/video/:id" Component={SingleVideo} />
        <Route path="/video/upload" Component={UploadVideo} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
