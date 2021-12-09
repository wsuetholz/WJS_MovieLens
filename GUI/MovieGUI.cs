using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using WJS_MovieLens.Model;
using WJS_MovieLens.Services;

namespace WJS_MovieLens.GUI
{
    public class MovieGUI : IDisposable
    {
        public Toplevel Top { get; set; }

        public Window Win { get; set; }

        TableView tableView;
        TableView genreView;

        User CurrentUser = null;

        string TitleFilter { get; set; }

        private bool disposedValue;

        public MovieGUI(Toplevel top)
        {
            Application.Init();

            TitleFilter = "";

            Top = top;
            if (Top == null)
            {
                Top = Application.Top;
            }

            Win = new Window($"Movie Lens Database -- CTRL-Q to Close")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            var menu = new MenuBar(new MenuBarItem[]
            {
                new MenuBarItem ("_File", new MenuItem[] {
                    new MenuItem ("_Quit", "", () => { if (Quit()) RequestStop(); })
                }),
                new MenuBarItem ("_Edit", new MenuItem[] {
                    new MenuItem ("_Add", "Add Video", () => { VideoAdd(); } ),
                    new MenuItem ("_Modify", "Modify Selected Video", () => { VideoEdit(); }),
                    new MenuItem ("_Delete", "Delete Selected Video", () => { VideoDelete(); } )
                }),
                new MenuBarItem ("_Filter", new MenuItem[] {
                    new MenuItem ("_Title", "Filter on Video Title", () => { SetTitleFilter(); PopulateTables(); }),
                    new MenuItem ("_Clear", "Clear All Filters", () => { ClearFilters(); PopulateTables(); })
                })
            });

            var statusBar = new StatusBar(new StatusItem[] {
                new StatusItem(Key.F1, "~F1~ Help", () => Help()),
                new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => { if (Quit ()) RequestStop(); })
            });

            tableView = new TableView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill() - 1,
            };

            tableView.FullRowSelect = true;
            tableView.Style.ColumnStyles.Clear();
            tableView.Style.AlwaysShowHeaders = true;
            tableView.CellActivated += EditVideo;
            tableView.MaxCellWidth = 40;

            tableView.Table = new DataTable();
            tableView.Table.Columns.Add("Id", typeof(long));
            tableView.Table.Columns.Add("Title", typeof(string));
            tableView.Table.Columns.Add("Genres", typeof(string));
            tableView.Table.Columns.Add("Min", typeof(long));
            tableView.Table.Columns.Add("Avg", typeof(long));
            tableView.Table.Columns.Add("Max", typeof(long));

            PopulateTables();   // Move to a background thread...

            //var scrollBar = new ScrollBarView(tableView, true);
            //scrollBar.ChangedPosition += () =>
            //{
            //    tableView.RowOffset = scrollBar.Position;
            //    if (tableView.RowOffset != scrollBar.Position)
            //    {
            //        scrollBar.Position = tableView.RowOffset;
            //    }
            //    tableView.SetNeedsDisplay();
            //};

            //tableView.DrawContent += (e) =>
            //{
            //    scrollBar.Size = tableView.Table?.Rows?.Count ?? 0;
            //    scrollBar.Position = tableView.RowOffset;
            //    scrollBar.Refresh();
            //};

            Win.Add(tableView);

            Top.Add(Win);
            Top.Add(menu, statusBar);
        }

        public void SetTitleFilter()
        {

            var oldValue = TitleFilter;
            bool okPressed = false;

            var ok = new Button("Ok", is_default: true);
            ok.Clicked += () => { okPressed = true; Application.RequestStop(); };
            var cancel = new Button("Cancel");
            cancel.Clicked += () => { Application.RequestStop(); };

            var d = new Dialog("Set Video Title Filter", 40, 10, ok, cancel);

            var lblT = new Label()
            {
                X = 0,
                Y = 1,
                Text = "Video Title Filter"
            };

            var tfT = new TextField()
            {
                Text = oldValue,
                X = 0,
                Y = 2,
                Width = Dim.Fill()
            };

            d.Add(lblT, tfT);
            tfT.SetFocus();

            Application.Run(d);


            if (okPressed)
            {
                TitleFilter = tfT.Text.ToString();
            }
        }
     
        public void ClearFilters()
        {
            TitleFilter = "";
        }

        public DataRow UpdateVideoRow(Movie movie, ref DataRow row)
        {
            row["Id"] = movie.Id;
            row["Title"] = movie.Title;

            string genres = "";
            foreach (var genre in movie.MovieGenres)
            {
                if (genres.Length < 1)
                {
                    genres = genre.Genre.Name;
                }
                else
                {
                    genres = $"{genres}, {genre.Genre.Name}";
                }
            }
            row["Genres"] = genres;

            long min = 999;
            long max = 0;
            long avg = 0;

            foreach (var rating in movie.UserMovies)
            {
                avg += rating.Rating;
                if (min > rating.Rating)
                {
                    min = rating.Rating;
                }
                if (max < rating.Rating)
                {
                    max = rating.Rating;
                }
            }
            if (movie.UserMovies.Count > 0)
                avg = avg / movie.UserMovies.Count;
            else
                min = 0;
            row["Min"] = min;
            row["Max"] = max;
            row["Avg"] = avg;

            return row;
        }

        public void PopulateVideoRow(Movie movie)
        {
            DataRow row = tableView.Table.NewRow();

            UpdateVideoRow(movie, ref row);

            tableView.Table.Rows.Add(row);
        }

        public void PopulateTables()
        {
            int rowCount = 0;
            tableView.Table.Clear();
            tableView.Update();

            using (var db = new DbMediaService())
            {
                var movieList = db.Movies.Where(x => x.Title.Contains(TitleFilter)).Include(x => x.MovieGenres).ThenInclude(x => x.Genre).Include(x => x.UserMovies);

                foreach (var movie in movieList)
                {
                    PopulateVideoRow(movie);
                }

                if (((rowCount++) % 40) == 0)
                {
                    tableView.Update();
                }
            }
            tableView.Update();

        }

        public void VideoAdd()
        {
            Movie movie = null;

            movie = AddUpdateMovie(movie, true);
            PopulateVideoRow(movie);
        }

        public void VideoEdit()
        {
            if (tableView == null || tableView.Table == null || tableView.SelectedRow < 0)
            {
                MessageBox.Query(50, 7, "Error", "No Selected Row to Edit.", "Ok");
                return;
            }

            long movieId = (long)tableView.Table.Rows[tableView.SelectedRow][0];

            Movie movie = null;
            using (var db = new DbMediaService())
            {
                movie = db.Movies.Include(x => x.MovieGenres).ThenInclude(x => x.Genre).Include(x => x.UserMovies).FirstOrDefault(movie => movie.Id == movieId);
            }

            if (movie == null)
            {
                MessageBox.Query(50, 7, "Error", "Movie not found in the database.", "Ok");
                return;
            }

            movie = AddUpdateMovie(movie, false);

            DataRow row = tableView.Table.Rows[tableView.SelectedRow];

            UpdateVideoRow(movie, ref row);

            tableView.Update();
        }

        public void VideoDelete()
        {
            if (tableView == null || tableView.Table == null || tableView.SelectedRow < 0)
            {
                MessageBox.Query(50, 7, "Error", "No Selected Row to Delete.", "Ok");
                return;
            }

            long movieId = (long)tableView.Table.Rows[tableView.SelectedRow][0];

            Movie movie = null;
            using (var db = new DbMediaService())
            {
                movie = db.Movies.Include(x => x.MovieGenres).ThenInclude(x => x.Genre).Include(x => x.UserMovies).FirstOrDefault(movie => movie.Id == movieId);
                if (movie != null)
                {
                    var n = MessageBox.Query(50, 7, "Delete Movie", $"Are you sure you want to delete the Movie \"{movie.Title}\"?", "Yes", "No");
                    if (n == 0)
                    {
                        foreach (var movieGenre in movie.MovieGenres)
                        {
                            db.Remove(movieGenre);
                        }
                        foreach (var userMovie in movie.UserMovies)
                        {
                            db.Remove(userMovie);
                        }
                        db.Remove(movie);

                        db.SaveChanges();

                        tableView.Table.Rows.Remove(tableView.Table.Rows[tableView.SelectedRow]);
                    }
                }
                else
                {
                    MessageBox.Query(50, 7, "Error", "Movie not found in the database.", "Ok");
                    return;
                }
            }

            tableView.Table.Rows.Remove(tableView.Table.Rows[tableView.SelectedRow]);
            tableView.Update();
        }

        public void EditVideo(TableView.CellActivatedEventArgs e)
        {
            long movieId = 0;
            Movie movie = null;

            if (e != null && e.Table != null)
            {
                movieId = (long)e.Table.Rows[e.Row][0];
            }

            if (movieId == 0)
            {
                MessageBox.Query(50, 7, "Error", "No Selected Row to Edit.", "Ok");
                return;
            }

            using (var db = new DbMediaService())
            {
                movie = db.Movies.Include(x => x.MovieGenres).ThenInclude(x => x.Genre).Include(x => x.UserMovies).FirstOrDefault(movie => movie.Id == movieId);
            }

            if (movie == null)
            {
                MessageBox.Query(50, 7, "Error", "Movie not found in the database.", "Ok");
                return;
            }

            movie = AddUpdateMovie(movie, false);

            DataRow row = tableView.Table.Rows[e.Row];

            UpdateVideoRow(movie, ref row);

            tableView.Update();
        }

        public void ToggleSelectedGenre(TableView.CellActivatedEventArgs e)
        {
            if (e != null && e.Table != null)
            {
                if ((bool)e.Table.Rows[e.Row][0])
                {
                    e.Table.Rows[e.Row][0] = false;
                }
                else
                {
                    e.Table.Rows[e.Row][0] = true;
                }
                genreView.Update();
            }

        }

        public Movie AddUpdateMovie(Movie movie, bool addMovie)
        {
            string oldValue = "";
            if (movie == null)
            {
                movie = new Movie();
                movie.Id = 0;
                movie.Title = "";
                movie.MovieGenres = new List<MovieGenre>();
                movie.UserMovies = new List<UserMovie>();
            }

            oldValue = movie.Title;

            var oldDate = movie.ReleaseDate;
            long oldRating = 0;

            bool okPressed = false;

            var ok = new Button("Ok", is_default: true);
            ok.Clicked += () => { okPressed = true; Application.RequestStop(); };
            var cancel = new Button("Cancel");
            cancel.Clicked += () => { Application.RequestStop(); };

            var d = new Dialog("Add/Modify Video Information", 60, 20, ok, cancel);

            var lblT = new Label()
            {
                X = 0,
                Y = 1,
                Text = "Video Title"
            };

            var tfT = new TextField()
            {
                Text = oldValue,
                X = 0,
                Y = 2,
                Width = Dim.Fill()
            };

            var lblR = new Label()
            {
                X = 0,
                Y = 3,
                Text = "Video Release Date"
            };

            var dfR = new DateField()
            {
                Date = oldDate,
                X = 0,
                Y = 4,
                Width = Dim.Fill()
            };

            var lblRate = new Label()
            {
                X = 0,
                Y = 5,
                Text = "Video Rating"
            };

            var tfRate = new TextField()
            {
                Text = oldRating.ToString(),
                X = 0,
                Y = 6,
                Width = Dim.Fill()
            };

            var lblG = new Label()
            {
                X = 0,
                Y = 7,
                Text = "Genres"
            };

            genreView = new TableView()
            {
                X = 0,
                Y = 8,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill() - 1,
            };

            genreView.FullRowSelect = true;
            genreView.Style.ColumnStyles.Clear();
            genreView.Style.AlwaysShowHeaders = true;
            genreView.CellActivated += ToggleSelectedGenre;
            genreView.MaxCellWidth = 40;

            genreView.Table = new DataTable();
            genreView.Table.Columns.Add("Selected", typeof(bool));
            genreView.Table.Columns.Add("Id", typeof(long));
            genreView.Table.Columns.Add("Genre", typeof(string));

            using (var db = new DbMediaService())
            {
                foreach (var genre in db.Genres)
                {
                    DataRow row = genreView.Table.NewRow();

                    row["Id"] = genre.Id;
                    row["Genre"] = genre.Name;
                    row["Selected"] = false;

                    foreach (var movieGenre in movie.MovieGenres)
                    {
                        if (movieGenre.Genre.Id == genre.Id)
                        {
                            row["Selected"] = true;
                            break;
                        }
                    }

                    genreView.Table.Rows.Add(row);
                }
            }

            d.Add(lblT, tfT);
            d.Add(lblR, dfR);
            if (CurrentUser != null)
                d.Add(lblRate, tfRate);
            d.Add(lblG, genreView);
            tfT.SetFocus();

            Application.Run(d);

            if (okPressed)
            {
                bool doUpdate = true;
                movie.Title = tfT.Text.ToString();
                movie.ReleaseDate = dfR.Date;

                using (var db = new DbMediaService())
                {
                    var checkTitle = db.Movies.FirstOrDefault(x => x.Title.Equals(movie.Title));
                    if (checkTitle != null) // Found a matching title
                    {
                        if (movie.Id > 0 && checkTitle.Id != movie.Id)
                        {
                            MessageBox.Query(50, 7, "Error", "Duplicate movie title found in the database.", "Ok");
                            doUpdate = false;
                        }
                    }

                }

                ICollection<MovieGenre> newMovieGenres = movie.MovieGenres;
                ICollection<UserMovie> newUserMovies = movie.UserMovies;

                using (var db = new DbMediaService())
                {
                    if (doUpdate && addMovie)
                    {
                        db.Add(movie);
                        db.SaveChanges();
                        var newMovie = db.Movies.FirstOrDefault(x => x.Title.Equals(movie.Title));
                        movie.Id = newMovie.Id;
                        foreach (DataRow row in genreView.Table.Rows)
                        {
                        
                            bool selected = (bool)row[0];
                            long genreId = (long)row[1];
                            if (selected)
                            {
                                var movieGenre = new MovieGenre();
                                movieGenre.Movie = movie;
                                movieGenre.Genre = db.Genres.FirstOrDefault(x => x.Id == genreId);

                                if (movieGenre.Genre != null)
                                {
                                    db.Add(movieGenre);
                                    newMovieGenres.Add(movieGenre);
                                }
                            }
                        }
                        if (CurrentUser != null)
                        {
                            UserMovie userMovie = new UserMovie();
                            userMovie.Movie = movie;
                            userMovie.RatedAt = DateTime.Now;
                            long rating = Int64.Parse(tfRate.Text.ToString());
                            if (rating > 5)
                                rating = 5;
                            else if (rating < 0)
                                rating = 0;
                            userMovie.Rating = rating;
                            db.Add(userMovie);

                            newUserMovies.Add(userMovie);
                        }
                    }
                    else if (doUpdate)
                    {
                        foreach (DataRow row in genreView.Table.Rows)
                        {

                            bool selected = (bool)row[0];
                            long genreId = (long)row[1];
                            bool found = false;
                            foreach (var mGenre in movie.MovieGenres)
                            {
                                if (mGenre.Genre.Id == genreId)
                                {
                                    found = true;
                                    if (!selected)
                                    {
                                        mGenre.Movie = null;
                                        mGenre.Genre = null;
                                        db.Remove<MovieGenre>(mGenre);
                                        newMovieGenres.Remove(mGenre);
                                    }
                                    else
                                    {
                                        newMovieGenres.Add(mGenre);
                                    }
                                }
                            }

                            if (!found && selected)
                            {
                                var movieGenre = new MovieGenre();
                                movieGenre.Movie = movie;
                                movieGenre.Genre = db.Genres.FirstOrDefault(x => x.Id == genreId);

                                if (movieGenre.Genre != null)
                                {
                                    db.Add(movieGenre);

                                    newMovieGenres.Add(movieGenre);
                                }
                            }
                        }

                        db.SaveChanges();

                        db.Update(movie);

                    }

                    db.SaveChanges();

                    movie.MovieGenres = newMovieGenres;
                    movie.UserMovies = newUserMovies;

                }
            }

            return movie;
        }

        public bool Quit()
        {
            var n = MessageBox.Query(50, 7, "Quit Movie Lens Program", "Are you sure you want to quit Movie Lens?", "Yes", "No");
            return n == 0;
        }

        public void Help()
        {
            MessageBox.Query(50, 7, "Help", "This is a small help\nBe kind.", "Ok");
        }

        public void Load()
        {
            MessageBox.Query(50, 7, "Load", "This is a small load\nBe kind.", "Ok");
        }

        public void Save()
        {
            MessageBox.Query(50, 7, "Save", "This is a small save\nBe kind.", "Ok");
        }

        public void Run()
        {
            
            Application.Run(Top);
        }

        public void RequestStop()
        {
            Application.RequestStop();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MovieGUI()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
