import React from "react";
import { useQuery } from "react-query";
import axios from "axios";
import VideoItem from "../components/VideoItem";

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
    <div>
      {data.map((video, id) => (
        <VideoItem key={id} video={video} />
      ))}
    </div>
  );
};

export default VideosPage;
