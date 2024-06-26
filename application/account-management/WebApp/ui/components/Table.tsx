import { ArrowUp } from "lucide-react";
import type {
  CellProps,
  ColumnProps,
  RowProps,
  TableHeaderProps,
  TableProps,
} from "react-aria-components";
import {
  Cell as AriaCell,
  Column as AriaColumn,
  Row as AriaRow,
  Table as AriaTable,
  TableHeader as AriaTableHeader,
  Button,
  Collection,
  ColumnResizer,
  Group,
  ResizableTableContainer,
  composeRenderProps,
  useTableOptions,
} from "react-aria-components";
import { twMerge } from "tailwind-merge";
import { tv } from "tailwind-variants";
import { Checkbox } from "./Checkbox";
import { composeTailwindRenderProps, focusRing } from "./utils";

export function Table(props: Readonly<TableProps>) {
  return (
    <ResizableTableContainer className="w-full relative">
      <AriaTable {...props} className="border-separate border-spacing-0" />
    </ResizableTableContainer>
  );
}

const columnStyles = tv({
  extend: focusRing,
  base: "px-2 h-5 flex-1 flex gap-1 items-center overflow-hidden",
});

const resizerStyles = tv({
  extend: focusRing,
  base: "w-px px-[8px] translate-x-[8px] box-content py-2 h-5 bg-clip-content forced-colors:bg-[ButtonBorder] cursor-col-resize rounded resizing:bg-blue-600 forced-colors:resizing:bg-[Highlight] resizing:w-[2px] resizing:pl-[7px] -outline-offset-2",
});

export function Column(props: Readonly<ColumnProps>) {
  return (
    <AriaColumn {...props} className={composeTailwindRenderProps(props.className, "[&:hover]:z-20 [&:focus-within]:z-20 text-start text-xs font-semibold text-gray-700 dark:text-zinc-300 cursor-default")}>
      {composeRenderProps(props.children, (children, { allowsSorting, sortDirection }) => (
        <div className="flex items-center">
          <Group
            role="presentation"
            tabIndex={-1}
            className={columnStyles}
          >
            <span className="truncate">{children}</span>
            {allowsSorting && (
              <span
                className={`w-4 h-4 flex items-center justify-center transition ${
                  sortDirection === "descending" ? "rotate-180" : ""
                }`}
              >
                {sortDirection && <ArrowUp aria-hidden className="w-4 h-4 text-gray-500 dark:text-zinc-400 forced-colors:text-[ButtonText]" />}
              </span>
            )}
          </Group>
          {!props.width && <ColumnResizer className={resizerStyles} />}
        </div>
      ))}
    </AriaColumn>
  );
}

export function TableHeader<T extends object>(props: Readonly<TableHeaderProps<T>>) {
  const { selectionBehavior, selectionMode, allowsDragging } = useTableOptions();

  return (
    <AriaTableHeader {...props} className={twMerge("shadow bg-gray-50 border-b", props.className)}>
      {/* Add extra columns for drag and drop and selection. */}
      {allowsDragging && <Column />}
      {selectionBehavior === "toggle" && (
        <AriaColumn width={36} minWidth={36} className="text-start text-sm font-semibold cursor-default p-2">
          {selectionMode === "multiple" && <Checkbox slot="selection" />}
        </AriaColumn>
      )}
      <Collection items={props.columns}>
        {props.children}
      </Collection>
    </AriaTableHeader>
  );
}

const rowStyles = tv({
  extend: focusRing,
  base: "group group/row relative cursor-default select-none -outline-offset-2 text-gray-900 disabled:text-gray-300 dark:text-zinc-200 dark:disabled:text-zinc-600 text-sm hover:bg-white dark:hover:bg-zinc-700/60 selected:bg-blue-100 dark:selected:hover:bg-blue-700/40 dark:selected:bg-blue-700/30 dark:selected:hover:bg-blue-700/40",
});

export function Row<T extends object>(
  { id, columns, children, ...otherProps }: Readonly<RowProps<T>>
) {
  const { selectionBehavior, allowsDragging } = useTableOptions();

  return (
    <AriaRow id={id} {...otherProps} className={rowStyles}>
      {allowsDragging && (
        <Cell>
          <Button slot="drag">≡</Button>
        </Cell>
      )}
      {selectionBehavior === "toggle" && (
        <Cell>
          <Checkbox slot="selection" />
        </Cell>
      )}
      <Collection items={columns}>
        {children}
      </Collection>
    </AriaRow>
  );
}

const cellStyles = tv({
  extend: focusRing,
  base: "border-b border-b-gray-200 group-last/row:border-b-0 [--selected-border:theme(colors.blue.200)] dark:[--selected-border:theme(colors.blue.900)] group-selected/row:border-[--selected-border] [:has(+[data-selected])_&]:border-[--selected-border] p-2 truncate -outline-offset-2",
});

export function Cell(props: Readonly<CellProps>) {
  return (
    <AriaCell {...props} className={cellStyles} />
  );
}
