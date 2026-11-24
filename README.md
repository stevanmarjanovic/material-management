# Material Management

A small inventory app for hobby sewing. Keep track of the fabrics you buy, see their properties at a glance, and record every piece you cut off, so you always know how much of each fabric is left and what each project actually used.

> Proof of concept, built for personal use. Runs locally, single user, no login.

## Features

**Materials**
- One material per purchase. Fabric is always bought at full roll width, so you enter the roll width and the bought length.
- Properties: name, color, weight (g/m²), certificate, link and photo.
- Composition as fibers with percentages (e.g. 95% Cotton, 5% Elastane), which must add up to 100%.
- Price per meter plus a discount %, both saved. The page shows the original price struck through next to the discounted one, and the total you paid.
- Needle type and size, shown with their color code.
- Care symbols (ISO 3758) for washing, bleaching, ironing, dry cleaning and tumble drying.
- Preview color: the average color of the photo, calculated in the browser and used to draw the pieces.
- Duplicate a material when you buy the same fabric again.

**Cutting**
- Every cut is a rectangle, so leftovers can become odd shapes, but they always have 90° corners.
- Drag a rectangle on the drawing of a piece (it snaps to 0.5 cm), then adjust X, Y, width and length by hand.
- A cut must lie fully on the fabric. If it splits a piece in two, each part becomes its own piece.
- A cut log records each cut's size, position, label and project. The last cut can be undone.

**Projects**
- Name, status (planned / in progress / done), start date, notes and a photo.
- Cuts can belong to a project, or to no project at all.
- The project overview lists the materials used: number of pieces, area in m², and cost based on the discounted price.
- Fiber composition of the whole project, by weight: cut area × g/m² × fiber %.

## Tech stack

| Part | Technology |
|---|---|
| API | .NET 10 minimal API, EF Core 10, SQLite |
| Web | Vue 3, Vite |
| Tests | xUnit |

No other runtime dependencies. Photos are stored in the database, so one file holds all your data.

## Getting started

**Requirements:** [.NET 10 SDK](https://dotnet.microsoft.com/download) and [Node.js](https://nodejs.org/) 20 or newer.

```bash
git clone https://github.com/stevanmarjanovic/material-management.git
cd material-management
cd web && npm install && cd ..
```

Start the API and the web app in two terminals:

```bash
# terminal 1
cd api
dotnet run
```

```bash
# terminal 2
cd web
npm run dev
```

Open http://localhost:5173. The API runs on http://localhost:5253, and Vite forwards `/api` requests to it.

The database `api/materials.db` is created automatically on first start. Back up that file to back up everything.

## Tests

```bash
dotnet test
```

The tests cover the cutting geometry: cutting holes, splitting a piece in two, rejecting cuts off the fabric, and using up a whole piece.

## Project structure

```
api/
  Program.cs        endpoints (materials, pieces, cuts, projects, photos)
  Data.cs           entities, enums, DbContext
  Geometry.cs       cutting: rectangle subtraction, splitting into connected pieces
  Migrations/       EF Core migrations, applied on startup
tests/              xUnit tests for Geometry
web/
  src/App.vue       main UI: materials, cutting, projects
  src/catalog.js    care values, needle types and sizes, project statuses
  src/components/   material and project forms, piece drawing, care icon, price tag
  public/care/      care symbol SVGs
```

## How cutting works

Each piece is stored as a list of non-overlapping rectangles in cm, which can describe any shape with only 90° corners.

1. The cut rectangle is subtracted from every rectangle in the piece. Each subtraction leaves up to 4 smaller rectangles.
2. The rectangles that remain are grouped into connected regions. Two rectangles are connected if they share an edge.
3. Each region becomes a new piece, moved so that it starts at 0,0.
4. The original piece is kept, marked inactive, so the last cut can be undone.

The browser draws a piece's outline by splitting it into a grid at every rectangle edge and drawing the lines where a filled cell meets an empty one.

## Changing the data model

After editing `api/Data.cs`, add a migration:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project api
```

It is applied automatically the next time the API starts.

## Credits

Care symbols are public domain SVGs from [Wikimedia Commons, Category:Laundry symbols](https://commons.wikimedia.org/wiki/Category:Laundry_symbols). Each file's source is listed in [`web/public/care/CREDITS.txt`](web/public/care/CREDITS.txt).

