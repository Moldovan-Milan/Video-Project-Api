import React from "react";
import VideoPlayer from "../components/VideoPlayer";
import { useParams } from "react-router-dom";

const SingleVideo = () => {
  const { id } = useParams();
  return (
    <div className="container">
      <VideoPlayer src={`https://localhost:7124/api/video/${id}`} id={id} />
    </div>
  );
};

export default SingleVideo;
