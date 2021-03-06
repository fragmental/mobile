using System;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Models
{
    public class ClientModel : Model
    {
        private static string GetPropertyName<T> (Expression<Func<ClientModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        private readonly int workspaceRelationId;

        public ClientModel ()
        {
            workspaceRelationId = ForeignRelation<WorkspaceModel> (PropertyWorkspaceId, PropertyWorkspace);
        }

        #region Data

        private string name;
        public static readonly string PropertyName = GetPropertyName ((m) => m.Name);

        [JsonProperty ("name")]
        public string Name {
            get {
                lock (SyncRoot) {
                    return name;
                }
            }
            set {
                lock (SyncRoot) {
                    if (name == value)
                        return;

                    ChangePropertyAndNotify (PropertyName, delegate {
                        name = value;
                    });
                }
            }
        }

        #endregion

        #region Relations

        public static readonly string PropertyWorkspaceId = GetPropertyName ((m) => m.WorkspaceId);

        public Guid? WorkspaceId {
            get { return GetForeignId (workspaceRelationId); }
            set { SetForeignId (workspaceRelationId, value); }
        }

        public static readonly string PropertyWorkspace = GetPropertyName ((m) => m.Workspace);

        [DontDirty]
        [SQLite.Ignore]
        [JsonProperty ("wid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
        public WorkspaceModel Workspace {
            get { return GetForeignModel<WorkspaceModel> (workspaceRelationId); }
            set { SetForeignModel (workspaceRelationId, value); }
        }

        public IModelQuery<ProjectModel> Projects {
            get { return Model.Query<ProjectModel> ((m) => m.ClientId == Id); }
        }

        #endregion
    }
}
