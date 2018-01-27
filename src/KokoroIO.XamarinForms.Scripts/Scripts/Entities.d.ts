interface MergeInfo {
    Id: number;
    IsMerged: boolean;
}
interface MessageInfo extends MergeInfo {
    IdempotentKey: string;
    ProfileId: string;
    Avatar: string;
    ScreenName: string;
    DisplayName: string;
    PublishedAt: string;
    IsBot: boolean;
    HtmlContent: string;
    EmbedContents: EmbedContent[];
    IsNsfw: boolean;
    CanDelete: boolean;
    IsDeleted: boolean;
}
interface EmbedContent {
    url: string;
    data: EmbedData;
}
interface EmbedData {
    type: "MixedContent" | "SingleImage" | "SingleVideo" | "SingleAudio";
    title: string;
    description: string;
    author_name: string;
    author_url: string;
    provider_name: string;
    provider_url: string;
    cache_age: number;
    metadata_image: EmbedDataMedia;
    url: string;
    restriction_policy: "Unknown" | "Safe" | "Restricted";
    medias: EmbedDataMedia[];
}
interface EmbedDataMedia {
    type: "Image" | "Video" | "Audio";
    thumbnail: EmbedDataImageInfo;
    raw_url: string;
    location: string;
    restriction_policy: "Unknown" | "Safe" | "Restricted";
}
interface EmbedDataImageInfo {
    url: string;
    width: number;
    height: number;
}