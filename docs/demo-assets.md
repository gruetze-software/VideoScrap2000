# Demo collection

The screenshot demo uses fictional movie and TV titles, plots, cast, studios, provider IDs and technical metadata. Provider IDs are sample values and are never queried against online services in demo mode. Empty video placeholders make the normal scanner and editor usable without distributing video content.

Original images were generated specifically for the demo: a mountain radio observatory, a northern harbor, a fictional cast portrait, and distinct movie-cover motifs featuring a sunlit sailing trip, a silver-and-violet extraterrestrial landscape, an emerald underwater wreck, a red-and-cyan rain-soaked station, and a warm bookbinder's room. The prompts requested original scenery and characters without real actors, existing film imagery, logos or franchises. Compressed JPEG sources are bundled in `MovieScrap2000/Assets/Demo`; posters and banners are composed locally with the fictional titles when the demo opens. No source images from TMDB or Fanart.tv are included in this demo.

Open the demo from Settings or start the application with `--demo`. It uses its own `Demo/v1` subfolder under the app's data directory, including a separate SQLite database. The normal collection settings are never saved from a demo session. Opening demo mode again restores the sample metadata.

For the AI-dialog screenshot, choose **The Last Signal → Metadata → Generate plot outline with AI**. This opens the regular prompt dialog; there is no automatic AI request or subscription requirement. The ChatGPT button opens a browser, while the text can also be pasted into another AI service.
