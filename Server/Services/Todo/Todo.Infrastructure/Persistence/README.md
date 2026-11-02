# Todo persistence

Implement the Todo database context, mappings, and migrations in this service. Resolve its database route using the authenticated tenant ID and the stable service ID `todo`. Keep tenant and owner filters in repository queries as defense in depth, even though the database is tenant-specific.
