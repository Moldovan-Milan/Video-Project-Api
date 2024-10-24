import React from "react";
import { useQuery } from "react-query";
import axios from "axios";
import VideoItem from "../components/VideoItem";
import { Link } from "react-router-dom";

const fetchVideos = async () => {
  const { data } = await axios.get("https://localhost:7124/api/video");
  return data;
};

const VideosPage = () => {
  const { data, error, isLoading } = useQuery(["videos"], fetchVideos);

  if (isLoading) return <div>Loading...</div>;
  if (error) return <div>Error: {error.message}</div>;

  console.log(data);

  return (
    <div className="row">
      <Link className="text-center btn btn-primary" to="/video/upload">
        Videó feltöltése
      </Link>
      <h1 className="text-center">Videók</h1>
      {data.map((video, id) => (
        <VideoItem key={id} video={video} />
      ))}
    </div>
  );
};

export default VideosPage;
