---
title: Turning Photos To HashTags with LLMs
layout: default
parent: Practical Examples and Applications
nav_order: 9
---

## Turning Photos To HashTags with LLMs

I'm going to create hashtags from images using SQL. We will use both Ollama for image description generation and OpenAI for hashtag generation.

## How It's Going To Work
- We're going to get image files from a specified directory.
- Each image will be converted to Base64.
- The local LLM will generate a description based on a prompt.
- The result will be stored as a column.
- We're going to contact OpenAI to generate hashtags from the given photo description.

This way, we can index photos and do something useful with them, like creating a thematic album.

## First Try
Here's an example SQL query using the `llama3.2-vision:latest` model to generate image descriptions:

```sql
select 
    f.Name, 
    l.AskImage('Describe this photo of my child in one sentence.', f.Base64File()) as description 
from @os.files('/some/folder/with/photos', false) f 
cross apply @ollama.llm('llama3.2-vision:latest') l
```

```
┌──────────────┬───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ f.Name       │ description                                                                                                                               │
├──────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ BEZN8348.JPG │ The baby in the image appears to be wearing a white long-sleeved shirt and brown pants, with a neutral expression on their face as they   │
│              │ are being held by an older man who is likely a grandparent.                                                                               │
│ AARD3200.JPG │ The baby in the image appears to be sleeping peacefully, lying on its back with its arms at its sides and a calm expression on its face.  │
│ CCLO3762.JPG │ The baby in the image appears to be sleeping peacefully in a stroller or bassinet, wrapped snugly in a white blanket and wearing a        │
│              │ matching hat.                                                                                                                             │
└──────────────┴───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

## Another Try With Commercial Model
For comparison, the following query uses the `gpt-4o` model to generate image descriptions:

```sql
select 
    f.Name, 
    l.AskImage('this is the photo of my little child I want you to describe. Be conscise, use only single statement.', f.Base64File()) as description 
from @os.files('/some/folder/with/photos', false) f 
cross apply @openai.gpt('gpt-4o') l
```

```
┌──────────────┬────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ f.Name       │ description                                                                                                            │
├──────────────┼────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ BEZN8348.JPG │ A man in a red sweater is holding a baby, with a decorative background featuring balloons and a banner.                │
│ CCLO3762.JPG │ A baby is peacefully sleeping in a stroller, bundled in warm white clothing, while an adult adjusts the blanket.       │
│ AARD3200.JPG │ A baby is peacefully sleeping, surrounded by patterned bedding and wearing a blue outfit with colorful vehicle prints. │
└──────────────┴────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

## Final Try With Both Models Combined
Because I don't want to send private photos (e.g., pictures of my child) to external services, I plan to generate photo descriptions using a local LLM model. By generating descriptions this way, I avoid leaking data that I don't want to show to the whole world. These generated descriptions are then sent to a much more powerful model which will generate the hashtags I request.

```sql
with PhotosDescription as (
    select 
        f.Name as Name, 
        l.AskImage('this is the photo of my little child I want you to describe. Be conscise, use only single statement.', f.Base64File()) as Description 
    from @os.files('/some/folder/with/photos', false) f 
    cross apply @ollama.llm('llama3.2-vision:11b-instruct-q4_K_M') l
)
select
    p.Name,
    p.Description,
    l.LlmPerform('this is the description of the photo I want you generate hashtags for. It comes from my child photo album. Return only hashtags separated with comma (#something, #somethingElse). Comma is very important to separate hashtags. Dont forget about it. No description or explanation.', p.Description) as HashTags
from PhotosDescription p cross apply @openai.gpt('gpt-4o', 4096, 0.0) l
```

```
┌──────────────┬─────────────────────────────────────────────────────────────────────┬─────────────────────────────────────────────────────────────────────┐
│ p.Name       │ p.Description                                                       │ HashTags                                                            │
├──────────────┼─────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────┤
│ AARD3200.JPG │ The baby in the image appears to be sleeping peacefully, lying on   │ #BabySleep, #PeacefulBaby, #SleepingBaby, #CalmBaby, #AdorableBaby, │
│              │ its back with its arms at its sides and a calm expression on its    │ #BabyAlbum, #SweetDreams, #InnocentSleep, #BabyMemories,            │
│              │ face.                                                               │ #CherishedMoments                                                   │
│ BEZN8348.JPG │ The baby in the image appears to be wearing a white long-sleeved    │ #BabyMemories, #FamilyLove, #GrandparentBond, #ChildhoodMoments,    │
│              │ shirt and brown pants, with a neutral expression on their face as   │ #FamilyAlbum, #PreciousMemories, #GenerationsTogether, #FamilyTime, │
│              │ they are being held by an older man who is likely a grandparent.    │ #BabyFashion, #CherishedMoments                                     │
│ CCLO3762.JPG │ The baby in the image appears to be sleeping peacefully in a        │ #BabySleep, #PeacefulBaby, #SleepingBaby, #SnugAsABug,              │
│              │ stroller or bassinet, wrapped snugly in a white blanket and wearing │ #BabyInBlanket, #AdorableBaby, #BabyInStroller, #Newborn, #BabyHat, │
│              │ a matching hat.                                                     │ #SweetDreams, #InnocentSleep, #BabyAlbum, #CherishedMoments,        │
│              │                                                                     │ #BabyMemories, #CozyBaby                                            │
└──────────────┴─────────────────────────────────────────────────────────────────────┴─────────────────────────────────────────────────────────────────────┘
```
